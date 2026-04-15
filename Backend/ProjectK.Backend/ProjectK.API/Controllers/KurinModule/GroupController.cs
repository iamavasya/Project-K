using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GroupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid groupKey)
        {
            var request = new GetGroupByKey(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("exists/{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Exists(Guid groupKey)
        {
            var request = new ExistsGroupByKey(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("groups")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "query:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid kurinKey)
        {
            var request = new GetGroups(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Create, "arg:request.KurinKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
        {
            var command = new UpsertGroup(request.Name, request.KurinKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPut("{groupKey:guid}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Update, "route:groupKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid groupKey, [FromBody] UpdateGroupRequest request)
        {
            var command = new UpsertGroup(groupKey, request.Name);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpDelete("{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Delete, "route:groupKey")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid groupKey)
        {
            var command = new DeleteGroup(groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
