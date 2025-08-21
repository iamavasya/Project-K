using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
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
    public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, ServiceResult<IEnumerable<MemberResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMembersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<MemberResponse>>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Member> members;

            if (request.KurinKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetAllAsync(request.GroupKey, cancellationToken);
            }
            else if (request.GroupKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken);
            }
            else
            {
                return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.BadRequest);
            }

            var response = _mapper.Map<IEnumerable<MemberResponse>>(members);
            
            return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, response);
        }
    }
}
