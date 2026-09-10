using MediatR;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Account
{
    public class ProvisionMemberAccountCommandHandler
        : IRequestHandler<ProvisionMemberAccountCommand, ServiceResult<Guid>>
    {
        private readonly IMemberUnitOfWork _unitOfWork;
        private readonly IAccountProvisioningService _accountProvisioning;
        private readonly IDomainEventPublisher _events;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IMembershipRepository _memberships;

        public ProvisionMemberAccountCommandHandler(
            IMemberUnitOfWork unitOfWork,
            IAccountProvisioningService accountProvisioning,
            IDomainEventPublisher events,
            IEmailService emailService,
            ICurrentUserContext currentUserContext,
            IUnitOfWork kurinData)
        {
            _unitOfWork = unitOfWork;
            _accountProvisioning = accountProvisioning;
            _events = events;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
            _memberships = kurinData.Memberships;
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

            // The account is opened for the kurin the person actually belongs to.
            var placement = await _memberships.GetActiveForMemberAsync(member.MemberKey, cancellationToken);
            var kurinKey = placement.FirstOrDefault()?.KurinKey ?? Guid.Empty;

            var provisioned = await _accountProvisioning.ProvisionAsync(
                new AccountProvisioningRequest(
                    member.Email,
                    member.FirstName,
                    member.LastName,
                    WaitlistEntryKey: null,
                    kurinKey,
                    IsBetaParticipant: true,
                    member.PhoneNumber,
                    member.DateOfBirth),
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
            await _events.PublishAsync(
                new MemberAccountLinked(member.MemberKey, provisioned.Data.UserKey),
                cancellationToken);

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
