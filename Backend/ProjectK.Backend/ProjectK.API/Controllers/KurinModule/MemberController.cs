using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members;
using ProjectK.Common.Extensions;
using ProjectK.Common.Helpers;
using ProjectK.Common.Models.Dtos.Requests;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MemberController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{memberKey}")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid memberKey)
        {
            var request = new GetMemberByKeyQuery(memberKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [HttpGet("members")]
        [ProducesResponseType(typeof(IEnumerable<MemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid groupKey = default, Guid kurinKey = default)
        {
            var request = new GetMembersQuery(groupKey, kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] UpsertMemberRequest request,
                                                CancellationToken cancellationToken)
        {
            byte[]? blobData = await ReadFileHelperFunction.ReadFileAsync(request.Blob, cancellationToken);
            var command = new UpsertMemberCommand
            {
                GroupKey = request.GroupKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                BlobContent = blobData,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [HttpPut("{memberKey:guid}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid memberKey,
                                                [FromForm] UpsertMemberRequest request,
                                                CancellationToken cancellationToken)
        {

            byte[]? blobData = await ReadFileHelperFunction.ReadFileAsync(request.Blob, cancellationToken);
            var command = new UpsertMemberCommand
            {
                MemberKey = memberKey,
                GroupKey = request.GroupKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                RemoveProfilePhoto = request.RemoveProfilePhoto ?? false,
                BlobContent = blobData,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [HttpDelete("{memberKey:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid memberKey)
        {
            var command = new DeleteMemberCommand(memberKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
