using MediatR;
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
using ProjectK.Common.Models.Dtos.Requests;

namespace ProjectK.API.Controllers.KurinModule
{
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

        [Authorize(Policy = "RequireUser")]
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

        [Authorize(Policy = "RequireUser")]
        [HttpGet("exists/{groupKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Exists(Guid groupKey)
        {
            var request = new ExistsGroupByKey(groupKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
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

        [Authorize(Policy = "RequireMentor")]
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

        [Authorize(Policy = "RequireMentor")]
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

        [Authorize(Policy = "RequireMentor")]
        [HttpPost("{groupKey:guid}/silhouette")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Update, "route:groupKey")]
        [RequestSizeLimit(MaxSilhouetteFileSizeBytes)]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadSilhouette(Guid groupKey, [FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "MissingImage", message = "Image file is required." });
            }

            if (file.Length > MaxSilhouetteFileSizeBytes)
            {
                return BadRequest(new { error = "ImageTooLarge", message = "Image file must be 5 MB or smaller." });
            }

            if (!AllowedSilhouetteContentTypes.Contains(file.ContentType))
            {
                return BadRequest(new { error = "UnsupportedImageType", message = "Allowed image types are PNG, JPEG and WebP." });
            }

            var bytes = await file.ToByteArrayAsync(cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return BadRequest(new { error = "MissingImage", message = "Image file is required." });
            }

            var command = new UploadGroupSilhouette(groupKey, bytes, file.FileName);
            var response = await _mediator.Send(command, cancellationToken);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
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

        [Authorize(Policy = "RequireManager")]
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

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{groupKey}/mentors")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Read, "route:groupKey")]
        [ProducesResponseType(typeof(IEnumerable<MemberLookupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMentors(Guid groupKey)
        {
            var query = new GetGroupMentorsQuery(groupKey);
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("groups/{kurinKey}/mentor-assignments")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<MentorAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKurinMentorAssignments(Guid kurinKey)
        {
            var query = new GetKurinMentorAssignmentsQuery(kurinKey);
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost("{groupKey}/mentors/{mentorUserKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Manage, "route:groupKey")]
        public async Task<IActionResult> AssignMentor(Guid groupKey, Guid mentorUserKey)
        {
            var command = new ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.AssignMentorCommand(mentorUserKey, groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpDelete("{groupKey}/mentors/{mentorUserKey}")]
        [ResourceAuthorize(ResourceType.Group, ResourceAction.Manage, "route:groupKey")]
        public async Task<IActionResult> RevokeMentor(Guid groupKey, Guid mentorUserKey)
        {
            var command = new ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.RevokeMentorCommand(mentorUserKey, groupKey);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
