using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers
{
    public class SubmitWaitlistRegistrationHandler : IRequestHandler<SubmitWaitlistRegistrationCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SubmitWaitlistRegistrationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<Guid>> Handle(SubmitWaitlistRegistrationCommand request, CancellationToken cancellationToken)
        {
            // Check for existing waitlist entry with same email
            var existingEntry = await _unitOfWork.WaitlistEntries.GetByEmailAsync(request.Email, cancellationToken);
            if (existingEntry != null)
            {
                return new ServiceResult<Guid>(ResultType.Conflict, Guid.Empty, "Waitlist entry with this email already exists.");
            }

            // Also check existing members just in case
            var memberByEmail = await _unitOfWork.Members.GetByEmailAsync(request.Email, cancellationToken);
            if (memberByEmail != null)
            {
                return new ServiceResult<Guid>(ResultType.Conflict, Guid.Empty, "A member with this email already exists in the system.");
            }

            var entry = new WaitlistEntry
            {
                WaitlistEntryKey = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                IsKurinLeaderCandidate = request.IsKurinLeaderCandidate,
                ClaimedKurinNameOrNumber = request.ClaimedKurinNameOrNumber,
                VerificationStatus = WaitlistVerificationStatus.Submitted,
                RequestedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.WaitlistEntries.Create(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Created, entry.WaitlistEntryKey);
        }
    }
}
