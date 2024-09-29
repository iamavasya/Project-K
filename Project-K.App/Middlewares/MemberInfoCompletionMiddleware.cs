using Microsoft.AspNetCore.Identity;
using Project_K.Infrastructure.Models;

namespace Project_K.Middlewares
{
    public class MemberInfoCompletionMiddleware
    {
        private readonly RequestDelegate _next;

        public MemberInfoCompletionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null && !user.IsMemberInfoCompleted && !context.Request.Path.StartsWithSegments("/DBView/Create"))
                {
                    context.Response.Redirect("/DBView/Create");
                    return;
                }
            }

            await _next(context);
        }
    }

}
