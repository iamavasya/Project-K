using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;

namespace ProjectK.API.Controllers.AuthModule
{
    [Authorize(Policy = "RequireAdmin")]
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
