using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.SeniorRecord;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.API.Controllers.KurinModule;

/// <summary>
/// The caller's own книжечка: ступені, відзначення and вишколи they enter themself. Read with the card
/// (<c>GET api/member/{key}</c>); written only here, and only one's own.
/// </summary>
[Route("api/me/senior-records")]
[Authorize(Policy = AuthorizationPolicies.RequireUser)]
[ApiController]
public class SeniorRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SeniorRecordsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MemberSeniorRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add([FromBody] UpsertMySeniorRecordCommand command)
    {
        command.MemberSeniorRecordKey = null;
        var result = await _mediator.Send(command);
        return result.ToActionResult(this);
    }

    [HttpPut("{key:guid}")]
    [ProducesResponseType(typeof(MemberSeniorRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid key, [FromBody] UpsertMySeniorRecordCommand command)
    {
        command.MemberSeniorRecordKey = key;
        var result = await _mediator.Send(command);
        return result.ToActionResult(this);
    }

    [HttpDelete("{key:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid key)
    {
        var result = await _mediator.Send(new DeleteMySeniorRecordCommand(key));
        return result.ToActionResult(this);
    }
}
