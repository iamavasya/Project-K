using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members
{
    public class GetMemberByKeyQuery : IRequest<ServiceResult<MemberResponse>>
    {
        public Guid MemberKey { get; set; }
        public GetMemberByKeyQuery(Guid memberKey)
        {
            MemberKey = memberKey;
        }
    }
}
