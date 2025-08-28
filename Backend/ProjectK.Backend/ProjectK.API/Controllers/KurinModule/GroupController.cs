using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups;
using ProjectK.Common.Extensions;
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

        [HttpGet("{groupKey}")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid groupKey)
        {
            var request = new GetGroupByKeyQuery(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [HttpGet("exists/{groupKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Exists(Guid groupKey)
        {
            var request = new ExistsGroupByKeyQuery(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
        

        [HttpGet("groups")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid kurinKey)
        {
            var request = new GetGroupsQuery(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [HttpPost]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
        {
            var command = new UpsertGroupCommand(request.Name, request.KurinKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [HttpPut("{groupKey:guid}")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid groupKey, [FromBody] UpdateGroupRequest request)
        {
            var command = new UpsertGroupCommand(groupKey, request.Name);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [HttpDelete("{groupKey}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid groupKey)
        {
            var command = new DeleteGroupCommand(groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
