using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands
{
    public class DeleteKurinCommand : IRequest<ServiceResult<object>>
    {
        public Guid KurinKey { get; set; }
        public DeleteKurinCommand(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }
}
