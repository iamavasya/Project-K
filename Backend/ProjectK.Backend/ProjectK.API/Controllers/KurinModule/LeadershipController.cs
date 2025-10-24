using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Leadership;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Leaderships;
using ProjectK.Common.Extensions;
using ProjectK.Common.Helpers;
using ProjectK.Common.Models.Dtos.Requests;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeadershipController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeadershipController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet]
        public async Task<IActionResult> Get(string leadershipType, Guid typeKey, CancellationToken cancellationToken)
        {
            var request = new GetLeadershipQuery(leadershipType, typeKey);
            var response = await _mediator.Send(request, cancellationToken);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPut("history/{historyKey}")]
        public async Task<IActionResult> UpdateHistory(Guid historyKey, [FromBody] UpsertLeadershipHistoryRequest request)
        {
            var command = new UpsertLeadershipHistoryCommand
            {

            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
