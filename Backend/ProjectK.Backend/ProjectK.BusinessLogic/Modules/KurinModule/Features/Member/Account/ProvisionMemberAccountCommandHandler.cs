using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Account
{
    public class ProvisionMemberAccountCommandHandler
        : IRequestHandler<ProvisionMemberAccountCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountProvisioningService _accountProvisioning;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserContext _currentUserContext;

        public ProvisionMemberAccountCommandHandler(
            IUnitOfWork unitOfWork,
            IAccountProvisioningService accountProvisioning,
            IEmailService emailService,
            ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _accountProvisioning = accountProvisioning;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<Guid>> Handle(
            ProvisionMemberAccountCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound);
            }

            if (member.UserKey.HasValue)
            {
                return new ServiceResult<Guid>(ResultType.Conflict);
            }

            var availability = await _accountProvisioning.CheckAvailabilityAsync(member.Email, cancellationToken);
            if (availability != AccountAvailability.Available)
            {
                return new ServiceResult<Guid>(ResultType.Conflict);
            }

            var now = DateTime.UtcNow;
            var waitlistEntry = new WaitlistEntry
            {
                WaitlistEntryKey = Guid.NewGuid(),
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                DateOfBirth = member.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                IsKurinLeaderCandidate = false,
                VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation,
                IsBetaParticipant = true,
                RequestedAtUtc = now,
                ReviewedAtUtc = now,
                ApprovedAtUtc = now,
                ReviewedByUserKey = _currentUserContext.UserId,
                InvitationSentAtUtc = now
            };

            _unitOfWork.WaitlistEntries.Create(waitlistEntry, cancellationToken);

            var provisioned = await _accountProvisioning.ProvisionAsync(
                new AccountProvisioningRequest(
                    member.Email,
                    member.FirstName,
                    member.LastName,
                    waitlistEntry.WaitlistEntryKey,
                    member.KurinKey,
                    IsBetaParticipant: true),
                cancellationToken);

            if (provisioned.Type != ResultType.Success || provisioned.Data is null)
            {
                return ServiceResult<Guid>.Failure(
                    provisioned.Type,
                    provisioned.ErrorCode ?? "UserNotCreated",
                    provisioned.ErrorMessage ?? "Failed to create user account for member.");
            }

            member.UserKey = provisioned.Data.UserKey;
            _unitOfWork.Members.Update(member, cancellationToken);

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<Guid>(ResultType.InternalServerError);
            }

            await _emailService.SendInvitationEmailAsync(
                member.Email,
                provisioned.Data.InvitationToken,
                cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, provisioned.Data.UserKey);
        }
    }
}
