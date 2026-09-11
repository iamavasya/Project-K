using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.ProbeAndBadges.Abstractions;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule
{
    /// <summary>
    /// The badge catalogue — the definitions themselves, identical for every kurin. Read-only: the
    /// catalogue ships with the application rather than being edited through it.
    /// </summary>
    [Route("api/catalog/badges")]
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [ApiController]
    public class BadgesCatalogController : ControllerBase
    {
        private readonly IBadgesCatalogService _badgesCatalogService;

        public BadgesCatalogController(IBadgesCatalogService badgesCatalogService)
        {
            _badgesCatalogService = badgesCatalogService;
        }

        /// <summary>
        /// Returns the catalogue's categories and levels, for building filters.
        /// </summary>
        [HttpGet("meta")]
        [ProducesResponseType(typeof(BadgesMetadata), StatusCodes.Status200OK)]
        public IActionResult GetMetadata()
        {
            return Ok(_badgesCatalogService.GetBadgesMetadata());
        }

        /// <summary>
        /// Lists badge definitions.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Badge>), StatusCodes.Status200OK)]
        public IActionResult GetAll([FromQuery] int take = 200)
        {
            // The catalog is a few hundred entries; a page past that is the whole thing, and a
            // non-positive request is a mistake answered with the default rather than nothing.
            var page = take <= 0 ? 200 : Math.Min(take, 500);
            return Ok(_badgesCatalogService.GetBadges(page));
        }

        /// <summary>
        /// Returns one badge definition.
        /// </summary>
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
