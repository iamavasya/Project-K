using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule
{
    [Route("api/catalog/probes")]
    [ApiController]
    public class ProbesCatalogController : ControllerBase
    {
        private readonly IProbesCatalogService _probesCatalogService;

        public ProbesCatalogController(IProbesCatalogService probesCatalogService)
        {
            _probesCatalogService = probesCatalogService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProbeSummaryResponse>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            return Ok(_probesCatalogService.GetProbes());
        }

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
