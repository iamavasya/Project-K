using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Planning;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Planning;
using ProjectK.Common.Extensions;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanningController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PlanningController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost]
        public async Task<IActionResult> CreatePlanningSession([FromBody] CreatePlanningSessionCommand request)
        {
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpGet("{planningSessionKey:guid}")]
        public async Task<IActionResult> GetPlanningSessionByKey(Guid planningSessionKey)
        {
            var request = new GetPlanningSessionByKeyQuery(planningSessionKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
