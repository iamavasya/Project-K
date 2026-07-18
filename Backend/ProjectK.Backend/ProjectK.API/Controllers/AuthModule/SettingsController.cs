using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Settings;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries.Settings;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SettingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var response = await _mediator.Send(new GetSystemSettingsQuery());
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            var response = await _mediator.Send(new UpdateSystemSettingCommand(key, request.Value));
            return response.ToActionResult(this);
        }
    }
    
    public record UpdateSettingRequest(string Value);
}
