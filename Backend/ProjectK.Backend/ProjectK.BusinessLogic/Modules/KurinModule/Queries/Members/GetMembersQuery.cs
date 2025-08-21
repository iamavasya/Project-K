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
    public class GetMembersQuery : IRequest<ServiceResult<IEnumerable<MemberResponse>>>
    {
        public Guid GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public GetMembersQuery(Guid groupKey, Guid kurinKey)
        {
            GroupKey = groupKey;
            KurinKey = kurinKey;
        }
    }
}
