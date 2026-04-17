using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Helpers;
using ProjectK.Common.Models.Enums;
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

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{memberKey}")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Read, "route:memberKey")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid memberKey)
        {
            var request = new GetMemberByKey(memberKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("groups/{groupKey:guid}/members")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllByGroup(Guid groupKey)
        {
            var request = new GetMembers(groupKey, Guid.Empty);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("kurins/{kurinKey:guid}/members")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllByKurin(Guid kurinKey)
        {
            var request = new GetMembers(Guid.Empty, kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Create, "arg:request.GroupKey")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] UpsertMemberRequest request,
                                                CancellationToken cancellationToken)
        {
            byte[]? blobData = await ReadFileHelperFunction.ReadFileAsync(request.Blob, cancellationToken);
            var command = new UpsertMember
            {
                GroupKey = request.GroupKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                PlastLevelHistories = request.PlastLevelHistories,
                BlobContent = blobData,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPut("{memberKey:guid}")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
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
            var command = new UpsertMember
            {
                MemberKey = memberKey,
                GroupKey = request.GroupKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                PlastLevelHistories = request.PlastLevelHistories,
                RemoveProfilePhoto = request.RemoveProfilePhoto ?? false,
                BlobContent = blobData,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpDelete("{memberKey:guid}")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Delete, "route:memberKey")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid memberKey)
        {
            var command = new DeleteMember(memberKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpGet("members/kv/{kurinKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        public async Task<IActionResult> GetKurinKvMembers(Guid kurinKey)
        {
            var request = new GetKurinKvMembers(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
