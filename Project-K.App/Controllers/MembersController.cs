using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Dtos;
using Microsoft.AspNetCore.Authorization;
using Project_K.BusinessLogic.Interfaces;

namespace Project_K.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MembersController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet]
        [Route("api/members")]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _memberService.GetMembersAsync();
            return Ok(members);
        }

        [HttpGet]
        [Route("api/members/{id}")]
        public async Task<IActionResult> GetMember(uint id)
        {
            var member = await _memberService.GetMember(id);
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

            var member = await _memberService.CreateMember(memberDto);

            return Ok(member);
        }

        [HttpPut]
        [Route("api/members/{id}")]
        public async Task<IActionResult> UpdateMember(uint id, [FromBody] MemberDto memberDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _memberService.UpdateMember(id, memberDto);

            return Ok();
        }

        [HttpDelete]
        [Route("api/members/{id}")]
        public async Task<IActionResult> DeleteMember(uint id)
        {
            await _memberService.DeleteMember(id);
            return Ok();
        }
    }
}
