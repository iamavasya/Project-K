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
    public class GetGroupByKeyQuery : IRequest<ServiceResult<GroupResponse>>
    {
        public Guid GroupKey { get; set; }
        public GetGroupByKeyQuery(Guid groupKey)
        {
            GroupKey = groupKey;
        }
    }
}
