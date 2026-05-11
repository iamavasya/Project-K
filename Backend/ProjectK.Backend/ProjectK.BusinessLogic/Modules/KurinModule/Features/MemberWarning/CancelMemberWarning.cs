using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning
{
    public sealed class CancelMemberWarning : IRequest<ServiceResult<MemberWarningDto>>
    {
        public CancelMemberWarning(Guid memberKey, Guid warningKey)
        {
            MemberKey = memberKey;
            WarningKey = warningKey;
        }

        public Guid MemberKey { get; }
        public Guid WarningKey { get; }
    }

    public sealed class CancelMemberWarningHandler : IRequestHandler<CancelMemberWarning, ServiceResult<MemberWarningDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IMapper _mapper;

        public CancelMemberWarningHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MemberWarningDto>> Handle(CancelMemberWarning request, CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.Unauthorized);
            }

            var warning = await _unitOfWork.MemberWarnings.GetByKeyAsync(request.WarningKey, cancellationToken);
            if (warning is null || warning.MemberKey != request.MemberKey)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.NotFound);
            }

            if (warning.RevokedAtUtc.HasValue || warning.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.Conflict);
            }

            if (warning.IssuedByUserKey != _currentUserContext.UserId.Value)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.Forbidden);
            }

            warning.RevokedAtUtc = DateTime.UtcNow;
            warning.RevokedByUserKey = _currentUserContext.UserId.Value;
            warning.UpdatedDate = warning.RevokedAtUtc.Value;

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<MemberWarningDto>(ResultType.InternalServerError);
            }

            var response = _mapper.Map<MemberWarningDto>(warning);
            return new ServiceResult<MemberWarningDto>(ResultType.Success, response);
        }
    }
}

