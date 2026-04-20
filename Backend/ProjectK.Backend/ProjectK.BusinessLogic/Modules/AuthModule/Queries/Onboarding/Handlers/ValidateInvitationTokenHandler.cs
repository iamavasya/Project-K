using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding.Handlers
{
    public class ValidateInvitationTokenHandler : IRequestHandler<ValidateInvitationTokenQuery, ServiceResult<InvitationValidationResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ValidateInvitationTokenHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<InvitationValidationResponse>> Handle(ValidateInvitationTokenQuery request, CancellationToken cancellationToken)
        {
            var invitation = await _unitOfWork.Invitations.GetByTokenAsync(request.Token, cancellationToken);

            if (invitation == null || invitation.ExpiresAtUtc < DateTime.UtcNow)
            {
                return new ServiceResult<InvitationValidationResponse>(
                    ResultType.NotFound, 
                    new InvitationValidationResponse("", "", "", false), 
                    "Invalid or expired invitation token.");
            }

            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(invitation.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return new ServiceResult<InvitationValidationResponse>(
                    ResultType.NotFound, 
                    new InvitationValidationResponse("", "", "", false), 
                    "Associated waitlist entry not found.");
            }

            return new ServiceResult<InvitationValidationResponse>(
                ResultType.Success, 
                new InvitationValidationResponse(entry.Email, entry.FirstName, entry.LastName, true));
        }
    }
}
