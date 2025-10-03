using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User
{
    public class LogoutUserCommand : IRequest<ServiceResult<object>>
    {
        public string? UserKey { get; set; }
        public LogoutUserCommand(string userKey)
        {
            UserKey = userKey;
        }
    }
}
