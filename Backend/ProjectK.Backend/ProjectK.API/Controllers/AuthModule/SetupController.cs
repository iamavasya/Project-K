using MediatR;
using ProjectK.Common.Models.Enums;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Get;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Initialize;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;

namespace ProjectK.API.Controllers.AuthModule
{
    /// <summary>
    /// First-run setup for a self-hosted instance. Anonymous, because on an empty database there is nobody
    /// to authenticate as yet.
    /// </summary>
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

        /// <summary>
        /// Reports whether the instance has been initialised.
        /// </summary>
        /// <remarks>
        /// The frontend reads this before showing a sign-in form, so a fresh instance sends the first visitor
        /// to setup instead of to a login they cannot pass.
        /// </remarks>
        [AllowAnonymous]
        [HttpGet("status")]
        [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Creates the first kurin and its administrator, and closes setup.
        /// </summary>
        /// <remarks>
        /// Refused once an administrator exists, so the endpoint cannot be used to add a second one to a
        /// running instance.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("initialize")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
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
