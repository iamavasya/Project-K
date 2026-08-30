using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Migration.PreflightReport;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.AuthModule
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [Route("api/auth/migration")]
    [ApiController]
    public class MigrationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MigrationController(IMediator _mediator)
        {
            this._mediator = _mediator;
        }

        [HttpGet("preflight")]
        public async Task<IActionResult> GetPreflightReport()
        {
            var query = new GetMigrationPreflightReportQuery();
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }
    }
}
