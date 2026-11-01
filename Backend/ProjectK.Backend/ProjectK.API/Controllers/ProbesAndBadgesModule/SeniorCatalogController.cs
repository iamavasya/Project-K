using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Catalog;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule;

/// <summary>The ступені, відзначення and вишколи a senior may enter in their книжечка. Read-only, from the catalogue.</summary>
[Route("api/catalog/senior")]
[Authorize(Policy = AuthorizationPolicies.RequireUser)]
[ApiController]
public class SeniorCatalogController : ControllerBase
{
    private readonly ISeniorRecordsCatalog _catalog;

    public SeniorCatalogController(ISeniorRecordsCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SeniorCatalogItem>), StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(_catalog.GetAll());
}
