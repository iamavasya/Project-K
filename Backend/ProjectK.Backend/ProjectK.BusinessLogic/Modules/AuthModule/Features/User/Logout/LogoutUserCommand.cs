using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Logout
{
    public class LogoutUserCommand : IRequest<ServiceResult<object>>
    {
        public string? UserKey { get; set; }

        /// <summary>
        /// The session being ended. An account can be signed in in several places, so signing out of
        /// this browser must not sign the person out of the others.
        /// </summary>
        public string? RefreshToken { get; set; }

        public LogoutUserCommand(string? userKey, string? refreshToken = null)
        {
            UserKey = userKey;
            RefreshToken = refreshToken;
        }
    }
}
