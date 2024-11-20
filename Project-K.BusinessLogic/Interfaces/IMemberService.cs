using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface IMemberService
    {
        Task<IEnumerable<Member>> GetMembersAsync();
        Task<Member> GetMember(uint id);
        Task<Member?> GetMemberDetailed(uint id);
        Task<MemberDto?> GetDto(uint id, string userId);
        Task<List<Member?>> GetMembersDetailed();
        Task<Member> CreateMember(MemberDto memberDto);
        Task<Member> CreateMember(MemberDto memberDto, string userId);

        Task<bool> UpdateMember(uint id, MemberDto memberDto);
        Task<bool> DeleteMember(uint id);
        Task<bool> IsMemberExists(uint id);
    }
}
