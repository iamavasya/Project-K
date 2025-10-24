using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Leadership
{
    public class UpsertLeadershipHistoryCommand : IRequest<ServiceResult<bool>>
    {
    }
}
