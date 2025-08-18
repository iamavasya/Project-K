using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups
{
    public class GetGroupsQuery : IRequest<ServiceResult<IEnumerable<GroupResponse>>>
    {
        public Guid KurinKey { get; set; }
        public GetGroupsQuery(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }
}
