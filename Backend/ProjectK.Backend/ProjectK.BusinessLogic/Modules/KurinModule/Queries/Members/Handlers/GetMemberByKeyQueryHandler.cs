using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members.Handlers
{
    public class GetMemberByKeyQueryHandler : IRequestHandler<GetMemberByKeyQuery, ServiceResult<MemberResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMemberByKeyQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MemberResponse>> Handle(GetMemberByKeyQuery request, CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<MemberResponse>(ResultType.NotFound);
            }
            var memberResponse = _mapper.Map<MemberResponse>(member);
            
            return new ServiceResult<MemberResponse>(ResultType.Success, memberResponse);
        }
    }
}
