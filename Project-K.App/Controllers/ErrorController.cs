using Microsoft.AspNetCore.Mvc;

namespace Project_K.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        [Route("Error/{statusCode}")]
        public IActionResult HandleError(int statusCode)
        {
            if (statusCode == 404)
            {
                return View("NotFound");
            }

            return View("Error");
        }
    }
}
