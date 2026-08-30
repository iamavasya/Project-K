using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Settings.Get;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Settings.Update;
using ProjectK.API.Authorization;

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

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var response = await _mediator.Send(new GetSystemSettingsQuery());
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            var response = await _mediator.Send(new UpdateSystemSettingCommand(key, request.Value));
            return response.ToActionResult(this);
        }
    }
    
    public record UpdateSettingRequest(string Value);
}
