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
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get
{
    public class GetMembers : IRequest<ServiceResult<IEnumerable<MemberResponse>>>
    {
        public Guid GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public GetMembers(Guid groupKey, Guid kurinKey)
        {
            GroupKey = groupKey;
            KurinKey = kurinKey;
        }
    }

    public class GetMembersHandler : IRequestHandler<GetMembers, ServiceResult<IEnumerable<MemberResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMembersHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<MemberResponse>>> Handle(GetMembers request, CancellationToken cancellationToken)
        {
            IEnumerable<MemberEntity> members;

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
