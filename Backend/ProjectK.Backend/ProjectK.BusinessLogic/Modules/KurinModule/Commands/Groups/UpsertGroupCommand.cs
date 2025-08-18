using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups
{
    public class UpsertGroupCommand : IRequest<ServiceResult<GroupResponse>>
    {
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Guid KurinKey { get; set; }

        public UpsertGroupCommand(Guid groupKey, string name)
        {
            GroupKey = groupKey;
            Name = name;
        }

        public UpsertGroupCommand(string name, Guid kurinKey)
        {
            KurinKey = kurinKey;
            Name = name;
        }
    }
}
