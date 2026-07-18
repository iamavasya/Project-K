using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Setup.Handlers
{
    public class GetSetupStatusQueryHandler : IRequestHandler<GetSetupStatusQuery, ServiceResult<SetupStatusResponse>>
    {
        private readonly UserManager<AppUser> _userManager;

        public GetSetupStatusQueryHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ServiceResult<SetupStatusResponse>> Handle(GetSetupStatusQuery request, CancellationToken cancellationToken)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(UserRole.Admin.ToString());
            var isInitialized = adminUsers.Any();
            return new ServiceResult<SetupStatusResponse>(ResultType.Success, new SetupStatusResponse(isInitialized));
        }
    }
}
