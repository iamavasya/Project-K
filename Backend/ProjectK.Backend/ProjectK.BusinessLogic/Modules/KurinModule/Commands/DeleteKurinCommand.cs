using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands
{
    public class DeleteKurinCommand : IRequest<bool>
    {
        public Guid KurinKey { get; set; }
        public DeleteKurinCommand(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }
}
