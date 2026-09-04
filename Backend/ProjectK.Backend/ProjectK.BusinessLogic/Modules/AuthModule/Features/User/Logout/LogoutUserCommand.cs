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
        /// Every refresh token this browser sent. An account can be signed in in several places, so
        /// signing out here must not sign the person out of the others.
        /// <para>
        /// A list, not one value: a browser can carry more than one <c>refreshToken</c> cookie — a
        /// stale one from an earlier session shadowing the live one — and reading only the first is
        /// how logout came to revoke the wrong session and leave the live one usable. Refresh has
        /// always walked all of them; so does this.
        /// </para>
        /// </summary>
        public IReadOnlyList<string> RefreshTokens { get; set; }

        public LogoutUserCommand(string? userKey, IReadOnlyList<string>? refreshTokens = null)
        {
            UserKey = userKey;
            RefreshTokens = refreshTokens ?? [];
        }
    }
}
