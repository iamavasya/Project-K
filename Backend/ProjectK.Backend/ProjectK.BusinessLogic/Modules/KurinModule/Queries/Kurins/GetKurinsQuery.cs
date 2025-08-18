using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins
{
    public class GetKurinsQuery : IRequest<ServiceResult<IEnumerable<KurinResponse>>>
    {
    }
}
