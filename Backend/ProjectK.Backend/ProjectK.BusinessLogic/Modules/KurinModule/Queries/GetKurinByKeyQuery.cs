using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;

namespace ProjectK.BusinessLogic.Modules.Kurin.Queries
{
    public class GetKurinByKeyQuery : IRequest<KurinResponse>
    {
        public Guid KurinKey { get; set; }
    }
}
