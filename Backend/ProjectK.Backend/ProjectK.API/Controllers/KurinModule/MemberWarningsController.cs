using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;

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

        [Authorize(Policy = "RequireUser")]
        [HttpGet]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Read, "route:memberKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberWarningDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWarnings(Guid memberKey)
        {
            var response = await _mediator.Send(new GetMemberWarnings(memberKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        [ProducesResponseType(typeof(MemberWarningDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignWarning(Guid memberKey, [FromBody] AssignMemberWarningRequest request)
        {
            var response = await _mediator.Send(new AssignMemberWarning(memberKey, request.Level));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpDelete("{warningKey:guid}")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
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
