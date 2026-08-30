using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning.Assign;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning.Cancel;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning.Get;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.KurinModule
{
    [ApiController]
    [Route("api/member/{memberKey:guid}/warnings")]
    public class MemberWarningsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MemberWarningsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet]
        [ResourceAuthorize(ResourceType.MemberWarning, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(typeof(IEnumerable<MemberWarningDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWarnings(Guid memberKey)
        {
            var response = await _mediator.Send(new GetMemberWarnings(memberKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpPost]
        [ResourceAuthorize(ResourceType.MemberWarning, ResourceAction.Create, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(typeof(MemberWarningDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignWarning(Guid memberKey, [FromBody] AssignMemberWarningRequest request)
        {
            var response = await _mediator.Send(new AssignMemberWarning(memberKey, request.Level));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpDelete("{warningKey:guid}")]
        [ResourceAuthorize(ResourceType.MemberWarning, ResourceAction.Update, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(typeof(MemberWarningDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelWarning(Guid memberKey, Guid warningKey)
        {
            var response = await _mediator.Send(new CancelMemberWarning(memberKey, warningKey));
            return response.ToActionResult(this);
        }
    }
}
