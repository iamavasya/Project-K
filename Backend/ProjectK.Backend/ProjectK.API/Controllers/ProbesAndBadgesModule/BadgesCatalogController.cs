using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule
{
    [Route("api/catalog/badges")]
    [Authorize(Policy = "RequireUser")]
    [ApiController]
    public class BadgesCatalogController : ControllerBase
    {
        private readonly IBadgesCatalogService _badgesCatalogService;

        public BadgesCatalogController(IBadgesCatalogService badgesCatalogService)
        {
            _badgesCatalogService = badgesCatalogService;
        }

        [HttpGet("meta")]
        [ProducesResponseType(typeof(BadgesMetadata), StatusCodes.Status200OK)]
        public IActionResult GetMetadata()
        {
            return Ok(_badgesCatalogService.GetBadgesMetadata());
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Badge>), StatusCodes.Status200OK)]
        public IActionResult GetAll([FromQuery] int take = 200)
        {
            return Ok(_badgesCatalogService.GetBadges(take));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Badge), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            var badge = _badgesCatalogService.GetBadgeById(id);
            return badge is null ? NotFound() : Ok(badge);
        }
    }
}
