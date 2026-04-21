using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding.Handlers
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

            var query = _userManager.Users.Where(u => u.IsBetaParticipant && u.OnboardingStatus == OnboardingStatus.Active);
            string? kurinName = null;
            string scope = "Global";

            var betaCapString = _configuration["Email:BetaCap"] ?? "10";
            int.TryParse(betaCapString, out var betaCap);
            if (betaCap == 0) betaCap = 10;

            if (kurinKey.HasValue)
            {
                query = query.Where(u => u.KurinKey == kurinKey.Value);
                var kurin = await _unitOfWork.Kurins.GetByKeyAsync(kurinKey.Value, cancellationToken);
                kurinName = kurin != null ? $"Kurin {kurin.Number}" : null;
                scope = "Kurin";
                
                if (kurin != null)
                {
                    betaCap = kurin.ZbtUserCap;
                }
            }

            var activeBetaUsersCount = await query.CountAsync(cancellationToken);

            var stats = new ZbtStatsDto
            {
                CurrentActiveUsers = activeBetaUsersCount,
                BetaCap = betaCap,
                KurinName = kurinName,
                Scope = scope
            };

            return new ServiceResult<ZbtStatsDto>(ResultType.Success, stats);
        }
    }
}
