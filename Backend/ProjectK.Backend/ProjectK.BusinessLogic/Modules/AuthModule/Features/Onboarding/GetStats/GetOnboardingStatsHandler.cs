using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.GetStats
{
    public class GetOnboardingStatsHandler : IRequestHandler<GetOnboardingStatsQuery, ServiceResult<ZbtStatsDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IUnitOfWork _unitOfWork;

        public GetOnboardingStatsHandler(
            UserManager<AppUser> userManager, 
            IConfiguration configuration,
            ICurrentUserContext currentUserContext,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _configuration = configuration;
            _currentUserContext = currentUserContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<ZbtStatsDto>> Handle(GetOnboardingStatsQuery request, CancellationToken cancellationToken)
        {
            var kurinKey = request.KurinKey;
            
            // If user is not admin, they can only see stats for their own Kurin
            if (!_currentUserContext.IsInRole("Admin"))
            {
                kurinKey = _currentUserContext.KurinKey;
            }

            string? kurinName = null;
            string scope = "Global";

            var isClosedBeta = _configuration.GetValue<bool>("Onboarding:IsClosedBeta", true);
            var betaCap = _configuration.GetValue<int>("Onboarding:GlobalBetaCap", 10);

            if (kurinKey.HasValue)
            {
                var kurin = await _unitOfWork.Kurins.GetByKeyAsync(kurinKey.Value, cancellationToken);
                kurinName = kurin != null ? $"Kurin {kurin.Number}" : null;
                scope = "Kurin";
                
                if (kurin != null)
                {
                    betaCap = kurin.ZbtUserCap;
                }
            }

            var activeBetaUsersCount = await _unitOfWork.Users.CountActiveBetaAsync(kurinKey, cancellationToken);

            // If not in closed beta, we can return effectively infinite cap or just signal it via a large number
            if (!isClosedBeta)
            {
                betaCap = 999999;
            }

            var stats = new ZbtStatsDto
            {
                CurrentActiveUsers = activeBetaUsersCount,
                BetaCap = betaCap,
                IsClosedBeta = isClosedBeta,
                KurinName = kurinName,
                Scope = scope
            };

            return new ServiceResult<ZbtStatsDto>(ResultType.Success, stats);
        }
    }
}
