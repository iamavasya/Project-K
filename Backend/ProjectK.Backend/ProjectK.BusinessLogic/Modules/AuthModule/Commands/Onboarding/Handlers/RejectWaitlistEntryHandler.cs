using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers
{
    public class RejectWaitlistEntryHandler : IRequestHandler<RejectWaitlistEntryCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;

        public RejectWaitlistEntryHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<Guid>> Handle(RejectWaitlistEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(request.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Waitlist entry not found.");
            }

            entry.VerificationStatus = WaitlistVerificationStatus.Rejected;
            entry.ReviewedAtUtc = DateTime.UtcNow;
            entry.ReviewedByUserKey = _currentUserContext.UserId;
            entry.VerificationNote = request.Note;

            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, entry.WaitlistEntryKey);
        }
    }
}
