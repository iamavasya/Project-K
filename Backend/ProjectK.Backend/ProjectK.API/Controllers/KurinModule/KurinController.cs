using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins;
using ProjectK.Common.Extensions;

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

        [Authorize(Roles = "Admin")]
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            return Ok("Admin here");
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey}")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid kurinKey)
        {
            var request = new GetKurinByKeyQuery(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet("kurins")]
        [ProducesResponseType(typeof(IEnumerable<KurinResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var request = new GetKurinsQuery();
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
            var request = new UpsertKurinCommand(kurinNumber);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPut("{kurinKey}")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upsert(Guid kurinKey, [FromBody] int kurinNumber)
        {
            var request = new UpsertKurinCommand(kurinKey, kurinNumber);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpDelete("{kurinKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid kurinKey)
        {
            var request = new DeleteKurinCommand(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
