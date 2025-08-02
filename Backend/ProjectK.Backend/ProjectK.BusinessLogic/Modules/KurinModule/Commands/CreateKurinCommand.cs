using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands
{
    public class CreateKurinCommand : IRequest<Guid>
    {
        public int Number { get; set; }
    }
}
