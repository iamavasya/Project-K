using AutoMapper;
using MediatR;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning
{
    public sealed class AssignMemberWarning : IRequest<ServiceResult<MemberWarningDto>>
    {
        public AssignMemberWarning(Guid memberKey, MemberWarningLevel level)
        {
            MemberKey = memberKey;
            Level = level;
        }

        public Guid MemberKey { get; }
        public MemberWarningLevel Level { get; }
    }

    public sealed class AssignMemberWarningHandler : IRequestHandler<AssignMemberWarning, ServiceResult<MemberWarningDto>>
    {
        private static readonly IReadOnlyDictionary<MemberWarningLevel, int> LevelDurationsInMonths =
            new Dictionary<MemberWarningLevel, int>
            {
                [MemberWarningLevel.Level1] = 3,
                [MemberWarningLevel.Level2] = 6,
                [MemberWarningLevel.Level3] = 12
            };

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public AssignMemberWarningHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserContext currentUserContext,
            INotificationService notificationService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MemberWarningDto>> Handle(AssignMemberWarning request, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(typeof(MemberWarningLevel), request.Level))
            {
                return new ServiceResult<MemberWarningDto>(ResultType.BadRequest);
            }

            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.Unauthorized);
            }

            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.NotFound);
            }

            var now = DateTime.UtcNow;
            var activeWarnings = await _unitOfWork.MemberWarnings
                .GetActiveByMemberKeyAsync(request.MemberKey, now, cancellationToken);

            var activeLevels = activeWarnings.Select(w => w.Level).ToHashSet();

            if (!CanAssignLevel(request.Level, activeLevels))
            {
                return new ServiceResult<MemberWarningDto>(ResultType.Conflict);
            }

            var expiresAtUtc = now.AddMonths(LevelDurationsInMonths[request.Level]);

            var warningEntity = new Common.Entities.KurinModule.MemberWarning
            {
                MemberKey = request.MemberKey,
                Level = request.Level,
                IssuedAtUtc = now,
                ExpiresAtUtc = expiresAtUtc,
                IssuedByUserKey = _currentUserContext.UserId.Value,
                UpdatedDate = now
            };

            _unitOfWork.MemberWarnings.Create(warningEntity, cancellationToken);

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.InternalServerError);
            }

            await NotifyMemberOwnerAsync(member, warningEntity, cancellationToken);

            var response = _mapper.Map<MemberWarningDto>(warningEntity);
            return new ServiceResult<MemberWarningDto>(ResultType.Created, response);
        }

        private static bool CanAssignLevel(MemberWarningLevel requestedLevel, IReadOnlySet<MemberWarningLevel> activeLevels)
        {
            return requestedLevel switch
            {
                MemberWarningLevel.Level1 => !activeLevels.Contains(MemberWarningLevel.Level1),
                MemberWarningLevel.Level2 => activeLevels.Contains(MemberWarningLevel.Level1) && !activeLevels.Contains(MemberWarningLevel.Level2),
                MemberWarningLevel.Level3 => activeLevels.Contains(MemberWarningLevel.Level2) && !activeLevels.Contains(MemberWarningLevel.Level3),
                _ => false
            };
        }

        private async Task NotifyMemberOwnerAsync(
            Common.Entities.KurinModule.Member member,
            Common.Entities.KurinModule.MemberWarning warning,
            CancellationToken cancellationToken)
        {
            if (!member.UserKey.HasValue)
            {
                return;
            }

            await _notificationService.NotifyAsync(
                new NotificationRequest
                {
                    RecipientUserKey = member.UserKey.Value,
                    Type = AppNotificationType.MemberWarningAssigned,
                    Severity = AppNotificationSeverity.Warn,
                    Title = "Пересторогу призначено",
                    Body = $"До вашого профілю додано {GetWarningLevelName(warning.Level)}.",
                    EntityType = "MemberWarning",
                    EntityKey = warning.MemberWarningKey,
                    Route = $"/member/{member.MemberKey}",
                    ActorUserKey = _currentUserContext.UserId,
                    DeduplicationKey = $"member-warning:{warning.MemberWarningKey}"
                },
                cancellationToken);
        }

        private static string GetWarningLevelName(MemberWarningLevel level) =>
            level switch
            {
                MemberWarningLevel.Level1 => "першу пересторогу",
                MemberWarningLevel.Level2 => "другу пересторогу",
                MemberWarningLevel.Level3 => "третю пересторогу",
                _ => "пересторогу"
            };
    }
}
