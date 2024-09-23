using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Dtos;

namespace Project_K.Controllers
{
    public class MemberLevelController : ControllerBase
    {
        private readonly KurinDbContext _context;

        public MemberLevelController(KurinDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("api/memberlevels")]
        public async Task<IActionResult> GetMemberLevels()
        {
            var memberLevels = await _context.MemberLevels.Include(ml => ml.Member)
                                                          .Include(ml => ml.Level).ToListAsync();
            return Ok(memberLevels);
        }

        [HttpGet]
        [Route("api/memberlevels/{id}")]
        public async Task<IActionResult> GetMemberLevel(uint id)
        {
            var memberLevel = await _context.MemberLevels.FindAsync(id);
            if (memberLevel == null)
            {
                return NotFound();
            }
            return Ok(memberLevel);
        }

        [HttpPost]
        [Route("api/memberlevels")]
        public async Task<IActionResult> CreateMemberLevel([FromBody] MemberLevelDto memberLevelDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            MemberLevel memberLevel = new()
            {
                Member = await _context.Members.FindAsync(memberLevelDto.MemberId),
                Level = await _context.Levels.FindAsync(memberLevelDto.LevelId),
                AchieveDate = memberLevelDto.AchieveDate
            };

            await _context.MemberLevels.AddAsync(memberLevel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMemberLevel), new { id = memberLevel.Id }, memberLevelDto);
        }

        [HttpPut]
        [Route("api/memberlevels/{id}")]
        public async Task<IActionResult> UpdateMemberLevel(uint id, [FromBody] MemberLevelDto memberLevelDto)
        {
            var memberLevel = await _context.MemberLevels.FindAsync(id);
            if (memberLevel == null)
            {
                return NotFound();
            }

            memberLevel.Member = await _context.Members.FindAsync(memberLevelDto.MemberId);
            memberLevel.Level = await _context.Levels.FindAsync(memberLevelDto.LevelId);
            memberLevel.AchieveDate = memberLevelDto.AchieveDate;

            await _context.SaveChangesAsync();

            return Ok(memberLevel);
        }

        [HttpDelete]
        [Route("api/memberlevels/{id}")]
        public async Task<IActionResult> DeleteMemberLevel(uint id)
        {
            var memberLevel = await _context.MemberLevels.FindAsync(id);
            if (memberLevel == null)
            {
                return NotFound();
            }

            _context.MemberLevels.Remove(memberLevel);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}