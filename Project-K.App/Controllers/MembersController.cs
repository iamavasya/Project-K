using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Project_K.Controllers
{
    [Authorize(Roles = "Admin")]
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
            var members = await _context.Members.Include(m => m.KurinLevel)
                                                .Include(m => m.MemberLevels).ToListAsync();
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

            Member member = new()
            {
                FirstName = memberDto.FirstName,
                LastName = memberDto.LastName,
                MiddleName = memberDto.MiddleName,
                Nickname = memberDto.Nickname,
                BirthDate = memberDto.BirthDate,
                Phone = memberDto.Phone,
                Email = memberDto.Email,
                Telegram = memberDto.Telegram,
                PlastJoin = memberDto.PlastJoin,
                Address = memberDto.Address,
                School = memberDto.School,
                KurinLevelId = memberDto.KurinLevelId
            };
            member.KurinLevel = await _context.KurinLevels.FindAsync(member.KurinLevelId);

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
        }

        [HttpPut]
        [Route("api/members/{id}")]
        public async Task<IActionResult> UpdateMember(uint id, [FromBody] MemberDto memberDto)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            member.FirstName = memberDto.FirstName;
            member.LastName = memberDto.LastName;
            member.MiddleName = memberDto.MiddleName;
            member.Nickname = memberDto.Nickname;
            member.BirthDate = memberDto.BirthDate;
            member.Phone = memberDto.Phone;
            member.Email = memberDto.Email;
            member.Telegram = memberDto.Telegram;
            member.PlastJoin = memberDto.PlastJoin;
            member.Address = memberDto.Address;
            member.School = memberDto.School;
            member.KurinLevelId = memberDto.KurinLevelId;

            _context.Members.Update(member);
            await _context.SaveChangesAsync();

            return Ok();
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
