using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups
{
    public class ExistsGroupByKeyQuery(Guid groupKey) : IRequest<ServiceResult<bool>>
    {
        public Guid GroupKey { get; set; } = groupKey;
    }
}
