using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Silhouette;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Dtos;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Assign;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Revoke;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;
using ProjectK.API.Authorization;
using ProjectK.API.Models.Requests;

namespace ProjectK.API.Controllers.KurinModule
{
    /// <summary>
    /// Groups within a kurin, and the mentors assigned to them.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private const long MaxSilhouetteFileSizeBytes = 5 * 1024 * 1024;
        private static readonly ISet<string> AllowedSilhouetteContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/webp"
        };

        private readonly IMediator _mediator;

        public GroupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns one group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid groupKey)
        {
            var request = new GetGroupByKey(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Answers whether a group exists, without returning it.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("exists/{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Exists(Guid groupKey)
        {
            var request = new ExistsGroupByKey(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists the groups of one kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("groups")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "query:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll(Guid kurinKey)
        {
            var request = new GetGroups(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Creates a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Create, "arg:request.KurinKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
        {
            var command = new UpsertGroup(request.Name, request.KurinKey, request.Description);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Rewrites a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpPut("{groupKey:guid}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Update, "route:groupKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid groupKey, [FromBody] UpdateGroupRequest request)
        {
            var command = new UpsertGroup(groupKey, request.Name, request.Description);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Stores the group's silhouette image.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpPost("{groupKey:guid}/silhouette")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Update, "route:groupKey")]
        [RequestSizeLimit(MaxSilhouetteFileSizeBytes)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadSilhouette(Guid groupKey, [FromForm] UploadImageRequest form, CancellationToken cancellationToken)
        {
            var file = form.File;
            if (file == null || file.Length == 0)
            {
                return this.Failure(ResultType.BadRequest, "MissingImage", "Image file is required.");
            }

            if (file.Length > MaxSilhouetteFileSizeBytes)
            {
                return this.Failure(ResultType.BadRequest, "ImageTooLarge", "Image file must be 5 MB or smaller.");
            }

            if (!AllowedSilhouetteContentTypes.Contains(file.ContentType))
            {
                return this.Failure(ResultType.BadRequest, "UnsupportedImageType", "Allowed image types are PNG, JPEG and WebP.");
            }

            var bytes = await file.ToByteArrayAsync(cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return this.Failure(ResultType.BadRequest, "MissingImage", "Image file is required.");
            }

            var command = new UploadGroupSilhouette(groupKey, bytes, file.FileName);
            var response = await _mediator.Send(command, cancellationToken);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Removes the group's silhouette image.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpDelete("{groupKey:guid}/silhouette")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Update, "route:groupKey")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSilhouette(Guid groupKey, CancellationToken cancellationToken)
        {
            var command = new DeleteGroupSilhouette(groupKey);
            var response = await _mediator.Send(command, cancellationToken);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Deletes a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [HttpDelete("{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Delete, "route:groupKey")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid groupKey)
        {
            var command = new DeleteGroup(groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists the mentors assigned to a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{groupKey}/mentors")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberLookupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMentors(Guid groupKey)
        {
            var query = new GetGroupMentorsQuery(groupKey);
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists mentor assignments across the whole kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("groups/{kurinKey}/mentor-assignments")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<MentorAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKurinMentorAssignments(Guid kurinKey)
        {
            var query = new GetKurinMentorAssignmentsQuery(kurinKey);
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Assigns a mentor to a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [HttpPost("{groupKey}/mentors/{mentorUserKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Manage, "route:groupKey")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> AssignMentor(Guid groupKey, Guid mentorUserKey)
        {
            var command = new AssignMentorCommand(mentorUserKey, groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Removes a mentor's assignment to a group.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [HttpDelete("{groupKey}/mentors/{mentorUserKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Manage, "route:groupKey")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> RevokeMentor(Guid groupKey, Guid mentorUserKey)
        {
            var command = new RevokeMentorCommand(mentorUserKey, groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
