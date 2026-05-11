using AutoMapper;
using MediatR;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward
{
    public sealed class UpsertMemberAward : IRequest<ServiceResult<MemberAwardDto>>
    {
        public Guid? MemberAwardKey { get; set; }
        public Guid MemberKey { get; set; }
        public MemberAwardLevel Level { get; set; }
        public DateTime DateAcquired { get; set; }
        public string? Note { get; set; }
    }

    public sealed class UpsertMemberAwardHandler : IRequestHandler<UpsertMemberAward, ServiceResult<MemberAwardDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IMapper _mapper;

        public UpsertMemberAwardHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MemberAwardDto>> Handle(UpsertMemberAward request, CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<MemberAwardDto>(ResultType.NotFound);
            }

            ProjectK.Common.Entities.KurinModule.MemberAward? existingAward = null;
            if (request.MemberAwardKey.HasValue)
            {
                existingAward = await _unitOfWork.MemberAwards.GetByKeyAsync(request.MemberAwardKey.Value, cancellationToken);
            }

            if (existingAward != null && existingAward.MemberKey == request.MemberKey)
            {
                existingAward.Level = request.Level;
                existingAward.KurinKey = member.KurinKey;
                existingAward.DateAcquired = request.DateAcquired;
                existingAward.Note = request.Note;
                existingAward.Status = BadgeProgressStatus.Submitted;
                existingAward.SubmittedAtUtc = DateTime.UtcNow;
                existingAward.SubmittedByUserKey = _currentUserContext.UserId;
                existingAward.ReviewedAtUtc = null;
                existingAward.ReviewedByUserKey = null;
                existingAward.UpdatedDate = DateTime.UtcNow;

                _unitOfWork.MemberAwards.Update(existingAward);
            }
            else
            {
                var newAward = new ProjectK.Common.Entities.KurinModule.MemberAward
                {
                    MemberKey = request.MemberKey,
                    KurinKey = member.KurinKey,
                    Level = request.Level,
                    DateAcquired = request.DateAcquired,
                    Note = request.Note,
                    Status = BadgeProgressStatus.Submitted,
                    SubmittedAtUtc = DateTime.UtcNow,
                    SubmittedByUserKey = _currentUserContext.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                _unitOfWork.MemberAwards.Create(newAward);
                existingAward = newAward;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<MemberAwardDto>(existingAward);
            return new ServiceResult<MemberAwardDto>(ResultType.Success, response);
        }
    }
}
