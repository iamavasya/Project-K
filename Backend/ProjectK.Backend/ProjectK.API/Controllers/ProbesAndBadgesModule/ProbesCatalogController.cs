using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule
{
    /// <summary>
    /// The probe catalogue — read-only definitions shared by every kurin.
    /// </summary>
    [Route("api/catalog/probes")]
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [ApiController]
    public class ProbesCatalogController : ControllerBase
    {
        private readonly IProbesCatalogService _probesCatalogService;

        public ProbesCatalogController(IProbesCatalogService probesCatalogService)
        {
            _probesCatalogService = probesCatalogService;
        }

        /// <summary>
        /// Lists probe definitions.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProbeSummaryResponse>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            return Ok(_probesCatalogService.GetProbes());
        }

        /// <summary>
        /// Returns one probe with its points grouped as they are presented in the book.
        /// </summary>
        [HttpGet("{probeId}/grouped")]
        [ProducesResponseType(typeof(GroupedProbeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetGroupedById(string probeId)
        {
            var groupedProbe = _probesCatalogService.GetGroupedProbeById(probeId);
            return groupedProbe is null ? NotFound() : Ok(groupedProbe);
        }
    }
}
