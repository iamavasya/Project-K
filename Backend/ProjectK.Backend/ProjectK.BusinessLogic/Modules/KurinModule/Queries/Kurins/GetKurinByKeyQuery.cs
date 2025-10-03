using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins
{
    public class GetKurinByKeyQuery : IRequest<ServiceResult<KurinResponse>>
    {
        public Guid KurinKey { get; set; }

        public GetKurinByKeyQuery(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }
}
