using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins
{
    public class UpsertKurinCommand : IRequest<ServiceResult<KurinResponse>>
    {
        public Guid KurinKey { get; set; }
        public int Number { get; set; }
        public UpsertKurinCommand(Guid kurinKey, int number)
        {
            KurinKey = kurinKey;
            Number = number;
        }
        public UpsertKurinCommand(int number)
        {
            Number = number;
        }
    }
}
