using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert;
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
        [HttpGet("type/{leadershipType}/{typeKey:guid}")]
        public async Task<IActionResult> GetLeadershipByType(string leadershipType, Guid typeKey, CancellationToken cancellationToken)
        {
            var request = new GetLeadershipByType(leadershipType, typeKey);
            var response = await _mediator.Send(request, cancellationToken);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpGet("{leadershipKey:guid}")]
        public async Task<IActionResult> GetLeadershipByKey(Guid leadershipKey)
        {
            var request = new GetLeadershipByKey(leadershipKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost]
        public async Task<IActionResult> CreateLeadership([FromBody] UpsertLeadershipRequest dto)
        {
            var request = new UpsertLeadership(dto);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPut("{leadershipKey:guid}")]
        public async Task<IActionResult> UpdateLeadership(Guid leadershipKey, [FromBody] UpsertLeadershipRequest dto)
        {
            var request = new UpsertLeadership(dto, leadershipKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpGet("histories/{leadershipKey}")]
        public async Task<IActionResult> GetLeadershipHistories(Guid leadershipKey)
        {
            var request = new GetLeadershipHistories(leadershipKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
