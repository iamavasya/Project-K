using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Project_K.Controllers
{

    [ServiceFilter(typeof(CheckDatabaseStateFilter))]
    public class SeedController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
