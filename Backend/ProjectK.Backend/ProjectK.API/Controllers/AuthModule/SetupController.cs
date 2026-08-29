using MediatR;
using ProjectK.Common.Models.Enums;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Get;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Initialize;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/auth/setup")]
    [ApiController]
    public class SetupController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHostEnvironment _environment;

        public SetupController(IMediator mediator, IHostEnvironment environment)
        {
            _mediator = mediator;
            _environment = environment;
        }

        private bool IsSelfHost => _environment.EnvironmentName == "SelfHost";

        [AllowAnonymous]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            if (!IsSelfHost)
            {
                // Cloud/dev environments seed their admin at startup, so setup is never pending there.
                return Ok(new SetupStatusResponse(true));
            }

            var response = await _mediator.Send(new GetSetupStatusQuery());
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializeSetupCommand command)
        {
            if (!IsSelfHost)
            {
                return this.Failure(ResultType.Forbidden, "SelfHostOnly", "Setup is only available for self-hosted deployments.");
            }

            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
