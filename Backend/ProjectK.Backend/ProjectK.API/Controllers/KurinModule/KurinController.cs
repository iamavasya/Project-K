using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class KurinController : ControllerBase
    {
        // TODO: Implement the necessary handlers for Kurin operations
        [HttpGet]
        public ActionResult<string> GetById(int id)
        {
            return $"Kurin with ID {id} retrieved successfully!";
        }

        [HttpPost]
        public ActionResult<string> Create([FromBody] string kurinData)
        {
            return $"Kurin created with data: {kurinData}";
        }

        [HttpPut("{id}")]
        public ActionResult<string> Update(int id, [FromBody] string kurinData)
        {
            return $"Kurin with ID {id} updated with data: {kurinData}";
        }

        [HttpDelete("{id}")]
        public ActionResult<string> Delete(int id)
        {
            return $"Kurin with ID {id} deleted successfully!";
        }
    }
}
