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
            var stanytsia = request.Stanytsia?.Trim();
            var regionOrCountry = request.RegionOrCountry?.Trim();

            if (string.IsNullOrWhiteSpace(stanytsia))
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "StanytsiaRequired", "Stanytsia is required.");
            }

            if (string.IsNullOrWhiteSpace(regionOrCountry))
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "RegionOrCountryRequired", "Region is required.");
            }

            if (stanytsia.Length > 120)
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "StanytsiaTooLong", "Stanytsia must be 120 characters or fewer.");
            }

            if (regionOrCountry.Length > 120)
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "RegionOrCountryTooLong", "Region must be 120 characters or fewer.");
            }

            if (!request.IsKurinLeaderCandidate)
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "KurinLeaderCandidateRequired", "Kurin leader confirmation is required.");
            }

            var claimedKurinNumber = request.ClaimedKurinNameOrNumber?.Trim();

            if (string.IsNullOrWhiteSpace(claimedKurinNumber))
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "ClaimedKurinNumberRequired", "Kurin number is required.");
            }

            if (!claimedKurinNumber.All(char.IsDigit) || !int.TryParse(claimedKurinNumber, out _))
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "ClaimedKurinNumberInvalid", "Kurin number must contain only digits.");
            }

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
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Stanytsia = stanytsia,
                RegionOrCountry = regionOrCountry,
                IsKurinLeaderCandidate = request.IsKurinLeaderCandidate,
                ClaimedKurinNameOrNumber = claimedKurinNumber,
                VerificationStatus = WaitlistVerificationStatus.Submitted,
                RequestedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.WaitlistEntries.Create(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Created, entry.WaitlistEntryKey);
        }
    }
}
