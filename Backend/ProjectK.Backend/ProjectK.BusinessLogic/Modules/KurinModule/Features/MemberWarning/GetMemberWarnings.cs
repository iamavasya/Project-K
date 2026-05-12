using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
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
    public sealed class GetMemberWarnings : IRequest<ServiceResult<IEnumerable<MemberWarningDto>>>
    {
        public GetMemberWarnings(Guid memberKey)
        {
            MemberKey = memberKey;
        }

        public Guid MemberKey { get; }
    }

    public sealed class GetMemberWarningsHandler : IRequestHandler<GetMemberWarnings, ServiceResult<IEnumerable<MemberWarningDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMemberWarningsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<MemberWarningDto>>> Handle(GetMemberWarnings request, CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<IEnumerable<MemberWarningDto>>(ResultType.NotFound);
            }

            var warnings = await _unitOfWork.MemberWarnings.GetByMemberKeyAsync(request.MemberKey, cancellationToken);

            var response = _mapper.Map<IEnumerable<MemberWarningDto>>(
                warnings.OrderByDescending(w => w.IssuedAtUtc)
            );

            return new ServiceResult<IEnumerable<MemberWarningDto>>(ResultType.Success, response);
        }
    }
}

