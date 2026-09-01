using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;
using ProjectK.API.Models.Requests;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;
using ProjectK.API.Authorization;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.API.Controllers.KurinModule
{
    /// <summary>
    /// Members: their records, the groups they belong to, and whether their profile has been checked by
    /// leadership.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MemberController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns one member.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Lists the members of one group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Lists the members of one kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Creates a member in a group, with an optional photo.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Create, "arg:request.GroupKey")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] UpsertMemberRequest request,
                                                CancellationToken cancellationToken)
        {
            if (!request.GroupKey.HasValue || request.GroupKey.Value == Guid.Empty)
            {
                return this.Failure(ResultType.BadRequest, "GroupKeyRequired", "groupKey is required.");
            }

            var command = new UpsertMember
            {
                GroupKey = request.GroupKey.Value,
                KurinKey = request.KurinKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                PlastLevelHistories = request.PlastLevelHistories,
                CreateUserAccount = request.CreateUserAccount,
                BlobContent = request.Blob is { Length: > 0 } ? request.Blob.OpenReadStream() : null,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Creates a member against a kurin rather than a specific group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPost("kurins/{kurinKey:guid}/members")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Create, "route:kurinKey")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateByKurin(Guid kurinKey,
            [FromForm] UpsertMemberRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpsertMember
            {
                KurinKey = kurinKey,
                GroupKey = null,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                PlastLevelHistories = request.PlastLevelHistories,
                CreateUserAccount = request.CreateUserAccount,
                BlobContent = request.Blob is { Length: > 0 } ? request.Blob.OpenReadStream() : null,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };

            var response = await _mediator.Send(command, cancellationToken);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Rewrites a member's record.
        /// </summary>
        /// <remarks>
        /// Members may edit their own profile; editing somebody else's needs rights over that member.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

            var command = new UpsertMember
            {
                MemberKey = memberKey,
                GroupKey = request.GroupKey,
                KurinKey = request.KurinKey,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                PlastLevelHistories = request.PlastLevelHistories,
                RemoveProfilePhoto = request.RemoveProfilePhoto ?? false,
                CreateUserAccount = request.CreateUserAccount,
                BlobContent = request.Blob is { Length: > 0 } ? request.Blob.OpenReadStream() : null,
                BlobFileName = request.Blob?.FileName,
                BlobContentType = request.Blob?.ContentType
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Marks a member's profile checked by leadership.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpPut("{memberKey:guid}/profile-verification")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyProfile(
            Guid memberKey,
            [FromBody] VerifyMemberProfileRequest? request,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(
                new VerifyMemberProfile(memberKey, request?.Note),
                cancellationToken);

            return response.ToActionResult(this);
        }

        /// <summary>
        /// Withdraws that check, sending the profile back for review.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpDelete("{memberKey:guid}/profile-verification")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetProfileVerification(
            Guid memberKey,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(
                new ResetMemberProfileVerification(memberKey),
                cancellationToken);

            return response.ToActionResult(this);
        }

        /// <summary>
        /// Deletes a member.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Lists the members who sit in the kurin's governing body.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("members/kv/{kurinKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberLookupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKurinKvMembers(Guid kurinKey)
        {
            var request = new GetKurinKvMembers(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists the members eligible to be assigned as mentors.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [HttpGet("members/mentor-candidates/{kurinKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberLookupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKurinMentorCandidates(Guid kurinKey)
        {
            var request = new GetKurinMentorCandidates(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
