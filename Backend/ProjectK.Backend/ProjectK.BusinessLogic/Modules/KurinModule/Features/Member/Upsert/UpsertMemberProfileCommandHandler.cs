using AutoMapper;
using MediatR;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using GroupEntity = ProjectK.Common.Entities.KurinModule.Group;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert
{
    public class UpsertMemberProfileCommandHandler
        : IRequestHandler<UpsertMemberProfileCommand, ServiceResult<MemberProfileWriteResult>>
    {
        private readonly IMemberUnitOfWork _unitOfWork;
        // A member still stores where they are placed, so writing one means reading a гурток. When
        // placement moves to Membership this dependency goes with it.
        private readonly IUnitOfWork _kurinData;
        private readonly IMapper _mapper;
        private readonly ICurrentUserContext _currentUserContext;

        public UpsertMemberProfileCommandHandler(
            IMemberUnitOfWork unitOfWork,
            IUnitOfWork kurinData,
            IMapper mapper,
            ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _kurinData = kurinData;
            _mapper = mapper;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<MemberProfileWriteResult>> Handle(
            UpsertMemberProfileCommand request,
            CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);

            // Someone without leadership may edit their own details but not move themselves between
            // гуртки or куріні, so the placement is taken back from the stored row.
            if (existing != null && !CanEditRestrictedFields())
            {
                request.GroupKey = existing.GroupKey;
                request.KurinKey = existing.KurinKey;
            }

            GroupEntity? group = null;
            if (request.GroupKey.HasValue && request.GroupKey.Value != Guid.Empty)
            {
                group = await _kurinData.Groups.GetByKeyAsync(request.GroupKey.Value, cancellationToken);
            }

            if (group == null && (!request.KurinKey.HasValue || request.KurinKey.Value == Guid.Empty))
            {
                return new ServiceResult<MemberProfileWriteResult>(ResultType.NotFound);
            }

            var wasProfileVerifiedCurrent =
                existing?.ProfileVerificationStatus == MemberProfileVerificationStatus.VerifiedCurrent;

            bool isCreated;
            string? previousPhotoBlobName = null;

            if (existing == null)
            {
                existing = _mapper.Map<MemberEntity>(request);
                existing.GroupKey = group?.GroupKey;
                existing.KurinKey = group?.KurinKey ?? request.KurinKey!.Value;
                existing.LatestPlastLevel = LatestLevelOf(existing.PlastLevelHistory);

                _unitOfWork.Members.Create(existing, cancellationToken);
                isCreated = true;
            }
            else
            {
                isCreated = false;
                var shouldMarkProfileStale = wasProfileVerifiedCurrent
                    && HasSignificantProfileChange(request, existing, group);

                var preserveLinkedUserEmail = false;
                string? linkedUserEmail = null;

                if (existing.UserKey.HasValue)
                {
                    var emailChanged = !string.Equals(existing.Email, request.Email, StringComparison.OrdinalIgnoreCase);
                    var phoneChanged = !string.Equals(existing.PhoneNumber, request.PhoneNumber, StringComparison.OrdinalIgnoreCase);

                    if ((emailChanged || phoneChanged) && !CanEditRestrictedFields() && !IsCurrentUserOwner(existing))
                    {
                        return ServiceResult<MemberProfileWriteResult>.Failure(
                            ResultType.BadRequest,
                            "ContactInfoLinked",
                            "Cannot change email or phone number for a member linked to an active user account. The user must update this via their account settings.");
                    }

                    preserveLinkedUserEmail = emailChanged && !IsAdmin();
                    linkedUserEmail = preserveLinkedUserEmail ? existing.Email : null;
                }

                previousPhotoBlobName = existing.ProfilePhotoBlobName;
                _mapper.Map(request, existing);

                if (preserveLinkedUserEmail)
                {
                    existing.Email = linkedUserEmail!;
                }

                existing.GroupKey = group?.GroupKey;
                existing.KurinKey = group?.KurinKey ?? request.KurinKey!.Value;

                if (shouldMarkProfileStale)
                {
                    existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedStale;
                }

                if (CanEditRestrictedFields())
                {
                    UpdatePlastLevelHistory(existing.MemberKey, request.PlastLevelHistories, existing.PlastLevelHistory);
                    existing.LatestPlastLevel = LatestLevelOf(existing.PlastLevelHistory);
                }

                _unitOfWork.Members.Update(existing, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<MemberProfileWriteResult>(ResultType.InternalServerError);
            }

            return new ServiceResult<MemberProfileWriteResult>(
                ResultType.Success,
                new MemberProfileWriteResult(
                    existing.MemberKey,
                    isCreated,
                    wasProfileVerifiedCurrent,
                    previousPhotoBlobName));
        }

        private bool CanEditRestrictedFields() => _currentUserContext.IsLeadership();

        private bool IsAdmin() => _currentUserContext.IsAdmin();

        private bool IsCurrentUserOwner(MemberEntity member) =>
            member.UserKey.HasValue
            && _currentUserContext.UserId.HasValue
            && member.UserKey.Value == _currentUserContext.UserId.Value;

        private static PlastLevel? LatestLevelOf(ICollection<PlastLevelHistory> history) =>
            history.OrderByDescending(entry => entry.DateAchieved).FirstOrDefault()?.PlastLevel;

        private static void UpdatePlastLevelHistory(
            Guid memberKey,
            ICollection<PlastLevelHistoryDto> plastLevelHistoryDto,
            ICollection<PlastLevelHistory> plastLevelHistory)
        {
            if (plastLevelHistoryDto == null || !plastLevelHistoryDto.Any())
            {
                plastLevelHistory.Clear();
                return;
            }

            var dtoDict = plastLevelHistoryDto
                .Where(dto => dto.PlastLevelHistoryKey.HasValue && dto.PlastLevelHistoryKey != Guid.Empty)
                .ToDictionary(dto => dto.PlastLevelHistoryKey!.Value);

            var entitiesToDelete = plastLevelHistory
                .Where(e => !dtoDict.ContainsKey(e.PlastLevelHistoryKey))
                .ToList();

            foreach (var entity in entitiesToDelete)
            {
                plastLevelHistory.Remove(entity);
            }

            foreach (var dto in plastLevelHistoryDto)
            {
                if (!dto.PlastLevelHistoryKey.HasValue || dto.PlastLevelHistoryKey == Guid.Empty)
                {
                    plastLevelHistory.Add(new PlastLevelHistory
                    {
                        MemberKey = memberKey,
                        PlastLevel = dto.PlastLevel,
                        DateAchieved = dto.DateAchieved
                    });
                }
                else
                {
                    var existingHistory = plastLevelHistory
                        .FirstOrDefault(e => e.PlastLevelHistoryKey == dto.PlastLevelHistoryKey);

                    if (existingHistory != null)
                    {
                        existingHistory.PlastLevel = dto.PlastLevel;
                        existingHistory.DateAchieved = dto.DateAchieved;
                    }
                }
            }
        }

        private bool HasSignificantProfileChange(
            UpsertMemberProfileCommand request,
            MemberEntity existing,
            GroupEntity? targetGroup)
        {
            var targetGroupKey = targetGroup?.GroupKey;
            var targetKurinKey = targetGroup?.KurinKey ?? request.KurinKey;

            return !string.Equals(existing.FirstName, request.FirstName, StringComparison.Ordinal)
                   || !string.Equals(existing.MiddleName ?? string.Empty, request.MiddleName ?? string.Empty, StringComparison.Ordinal)
                   || !string.Equals(existing.LastName, request.LastName, StringComparison.Ordinal)
                   || !string.Equals(existing.Email, request.Email, StringComparison.OrdinalIgnoreCase)
                   || !string.Equals(existing.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal)
                   || existing.DateOfBirth != request.DateOfBirth
                   || !string.Equals(existing.Address ?? string.Empty, request.Address ?? string.Empty, StringComparison.Ordinal)
                   || !string.Equals(existing.School ?? string.Empty, request.School ?? string.Empty, StringComparison.Ordinal)
                   || existing.GroupKey != targetGroupKey
                   || existing.KurinKey != targetKurinKey
                   || (CanEditRestrictedFields() && HasPlastLevelHistoryChange(request.PlastLevelHistories, existing.PlastLevelHistory));
        }

        private static bool HasPlastLevelHistoryChange(
            ICollection<PlastLevelHistoryDto> requested,
            ICollection<PlastLevelHistory> existing)
        {
            if (requested.Count != existing.Count)
            {
                return true;
            }

            var existingByKey = existing.ToDictionary(history => history.PlastLevelHistoryKey);
            foreach (var dto in requested)
            {
                if (!dto.PlastLevelHistoryKey.HasValue || dto.PlastLevelHistoryKey == Guid.Empty)
                {
                    return true;
                }

                if (!existingByKey.TryGetValue(dto.PlastLevelHistoryKey.Value, out var entity))
                {
                    return true;
                }

                if (entity.PlastLevel != dto.PlastLevel || entity.DateAchieved != dto.DateAchieved)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
