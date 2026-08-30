using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.ProbeAndBadges.Abstractions;
using System;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Review;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Upsert;
using ProjectK.Common.Models.Dtos.ProbesAndBadgesModule.Requests;
using ProjectK.API.Authorization;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.API.Controllers.KurinModule
{
    [ApiController]
    [Route("api/member/{memberKey:guid}/awards")]
    // Says what it means: authenticated. The scope check is the ResourceAuthorize filter on each action.
    /// <summary>
    /// Awards recorded against a member, and the images that illustrate them.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    public class MemberAwardsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAwardImagesStore _awardImagesStore;

        public MemberAwardsController(IMediator mediator, IAwardImagesStore awardImagesStore)
        {
            _mediator = mediator;
            _awardImagesStore = awardImagesStore;
        }

        /// <summary>
        /// Records an award for a member, or rewrites one already recorded.
        /// </summary>
        [HttpPost]
        [ResourceAuthorize(ResourceType.MemberAward, ResourceAction.Create, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(typeof(MemberAwardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertAward(Guid memberKey, [FromBody] UpsertMemberAward command)
        {
            command.MemberKey = memberKey;
            var result = await _mediator.Send(command);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Confirms or refuses a recorded award. Leadership only.
        /// </summary>
        [HttpPost("{awardKey:guid}/review")]
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [ResourceAuthorize(ResourceType.MemberAward, ResourceAction.Update, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(typeof(MemberAwardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReviewAward(Guid memberKey, Guid awardKey, [FromBody] ReviewBadgeProgressRequest request)
        {
            var result = await _mediator.Send(new ReviewMemberAward
            {
                MemberAwardKey = awardKey,
                IsApproved = request.IsApproved
            });
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Removes an award.
        /// </summary>
        /// <remarks>
        /// A confirmed award can only be removed by leadership — the same bar that confirming it required. An
        /// unconfirmed one the member entered themselves, they can withdraw.
        /// </remarks>
        [HttpDelete("{awardKey:guid}")]
        [ResourceAuthorize(ResourceType.MemberAward, ResourceAction.Delete, "route:memberKey", ResourceType.Member)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAward(Guid memberKey, Guid awardKey)
        {
            var result = await _mediator.Send(new DeleteMemberAward { MemberAwardKey = awardKey });
            return result.ToActionResult(this);
        }

        [AllowAnonymous] // or keep Authorize if needed
        /// <summary>
        /// Serves the illustration for an award level, coloured or plain.
        /// </summary>
        [HttpGet("/api/awards/images/{level}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAwardImage(int level, [FromQuery] bool colored = true)
        {
            var stream = _awardImagesStore.GetAwardImageStream(level, colored);
            if (stream == null)
            {
                return this.Failure(ResultType.NotFound, "AwardImageNotFound", "No award image exists for this level.");
            }

            return File(stream, "image/png");
        }
    }
}
