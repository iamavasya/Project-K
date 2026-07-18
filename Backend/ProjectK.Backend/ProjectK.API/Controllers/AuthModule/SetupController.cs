using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Setup;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries.Setup;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/auth/setup")]
    [ApiController]
    public class SetupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SetupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var response = await _mediator.Send(new GetSetupStatusQuery());
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializeSetupCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
