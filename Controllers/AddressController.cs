using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Data;
using Project_K.Models;
using Project_K.Dtos;

namespace Project_K.Controllers
{
    public class AddressController : ControllerBase
    {
        private readonly KurinDbContext _context;

        public AddressController(KurinDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("api/addresses")]
        public async Task<IActionResult> GetAddress()
        {
            var addresses = await _context.Addresses.ToListAsync();
            return Ok(addresses);
        }

        [HttpPost]
        [Route("api/addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] AddressDto addressDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var address = new Address
            {
                AddressName = addressDto.Name
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, address);
        }
    }
}
