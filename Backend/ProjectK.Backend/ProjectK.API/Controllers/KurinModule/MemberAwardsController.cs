using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.ProbeAndBadges.Abstractions;
using System;
using System.Threading.Tasks;

namespace ProjectK.API.Controllers.KurinModule
{
    [ApiController]
    [Route("api/member/{memberKey:guid}/awards")]
    [Authorize]
    public class MemberAwardsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAwardImagesStore _awardImagesStore;

        public MemberAwardsController(IMediator mediator, IAwardImagesStore awardImagesStore)
        {
            _mediator = mediator;
            _awardImagesStore = awardImagesStore;
        }

        [HttpPost]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        public async Task<IActionResult> UpsertAward(Guid memberKey, [FromBody] UpsertMemberAward command)
        {
            command.MemberKey = memberKey;
            var result = await _mediator.Send(command);
            return result.ToActionResult(this);
        }

        [HttpPost("{awardKey:guid}/review")]
        [Authorize(Policy = "RequireMentor")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        public async Task<IActionResult> ReviewAward(Guid memberKey, Guid awardKey, [FromBody] ReviewBadgeProgressRequest request)
        {
            var result = await _mediator.Send(new ReviewMemberAward
            {
                MemberAwardKey = awardKey,
                IsApproved = request.IsApproved
            });
            return result.ToActionResult(this);
        }

        [HttpDelete("{awardKey:guid}")]
        [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
        public async Task<IActionResult> DeleteAward(Guid memberKey, Guid awardKey)
        {
            var result = await _mediator.Send(new DeleteMemberAward { MemberAwardKey = awardKey });
            return result.ToActionResult(this);
        }

        [AllowAnonymous] // or keep Authorize if needed
        [HttpGet("/api/awards/images/{level}")]
        public IActionResult GetAwardImage(int level, [FromQuery] bool colored = true)
        {
            var stream = _awardImagesStore.GetAwardImageStream(level, colored);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, "image/png");
        }
    }
}
