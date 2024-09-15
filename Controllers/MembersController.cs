using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Data;
using Project_K.Models;
using Project_K.Dtos;

namespace Project_K.Controllers
{
    public class MembersController : ControllerBase
    {
        private readonly KurinDbContext _context;

        public MembersController(KurinDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("api/members")]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _context.Members.Include(m => m.Address).ToListAsync();
            return Ok(members);
        }

        [HttpGet]
        [Route("api/members/{id}")]
        public async Task<IActionResult> GetMember(uint id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            return Ok(member);
        }

        [HttpPost]
        [Route("api/members")]
        public async Task<IActionResult> CreateMember([FromBody] MemberDto memberDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: навігаційна властивість
            Member member = new()
            {
                FirstName = memberDto.FirstName,
                LastName = memberDto.LastName,
                MiddleName = memberDto.MiddleName,
                BirthDate = memberDto.BirthDate,
                Phone = memberDto.Phone,
                Email = memberDto.Email,
                Telegram = memberDto.Telegram,
                PlastJoin = memberDto.PlastJoin,
                AddressId = memberDto.AddressId,
            };
            member.Address = await _context.Addresses.FindAsync(member.AddressId);
            /*member.School = await _context.Schools.FindAsync(member.SchoolId);
            member.KurinLevel = await _context.KurinLevels.FindAsync(member.KurinLevelId);*/

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
        }

        [HttpPut]
        [Route("api/members/{id}")]
        public async Task<IActionResult> UpdateMember(uint id, [FromBody] Member member)
        {
            if (id != member.Id)
            {
                return BadRequest();
            }

            _context.Entry(member).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Members.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete]
        [Route("api/members/{id}")]
        public async Task<IActionResult> DeleteMember(uint id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
