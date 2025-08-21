using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members
{
    public class DeleteMemberCommand : IRequest<ServiceResult<object>>
    {
        public Guid MemberKey { get; set; }
        public DeleteMemberCommand(Guid memberKey)
        {
            MemberKey = memberKey;
        }
    }
}
