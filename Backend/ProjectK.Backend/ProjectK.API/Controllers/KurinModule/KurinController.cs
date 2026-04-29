using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class KurinController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KurinController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpGet("{kurinKey:guid}/badges/review")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<BadgeProgressResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBadgeReviewQueue(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetBadgeReviewQueue(kurinKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid kurinKey)
        {
            var request = new GetKurinByKey(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet("kurins")]
        [ProducesResponseType(typeof(IEnumerable<KurinResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var request = new GetKurins();
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] int kurinNumber)
        {
            var request = new UpsertKurin(kurinNumber);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPut("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "route:kurinKey")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upsert(Guid kurinKey, [FromBody] int kurinNumber)
        {
            var request = new UpsertKurin(kurinKey, kurinNumber);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpDelete("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Delete, "route:kurinKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid kurinKey)
        {
            var request = new DeleteKurin(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
