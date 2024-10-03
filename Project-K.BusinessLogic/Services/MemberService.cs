using Microsoft.AspNetCore.Mvc;
using Project_K.BusinessLogic.Interfaces;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;
using Project_K.Infrastructure.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Project_K.BusinessLogic.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IKurinLevelRepository _kurinLevelRepository;
        private readonly ITeamRepository _teamRepository;

        public MemberService(IMemberRepository memberRepository, IKurinLevelRepository kurinLevelRepository, ITeamRepository teamRepository)
        {

            _memberRepository = memberRepository;
            _kurinLevelRepository = kurinLevelRepository;
            _teamRepository = teamRepository;
        }

        public async Task<IEnumerable<Member>> GetMembersAsync()
        {
            return await _memberRepository.GetMembersAsync();
        }

        public async Task<Member> GetMember(uint id)
        {
            return await _memberRepository.GetByIdAsync(id);
        }

        public async Task<Member> CreateMember(MemberDto memberDto)
        {
            Member member = MapMemberDtoToMember(memberDto);
            member.KurinLevel = await _kurinLevelRepository.GetByIdAsync(member.KurinLevelId);
            member.Team = await _teamRepository.GetByIdAsync(member.TeamId);
            await _memberRepository.AddAsync(member);
            return member;
        }

        public async Task<bool> UpdateMember(uint id, MemberDto memberDto)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return false;
            }

            MapMemberDtoToMember(memberDto, member);
            await _memberRepository.UpdateAsync(member);
            return true;
        }

        public async Task<bool> DeleteMember(uint id)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return false;
            }

            await _memberRepository.DeleteAsync(member);
            return true;
        }

        private Member MapMemberDtoToMember(MemberDto memberDto, Member member = null)
        {
            if (member == null)
            {
                member = new()
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
                    TeamId = memberDto.TeamId,
                    KurinLevelId = memberDto.KurinLevelId
                };
            } else
            {
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
                member.TeamId = memberDto.TeamId;
            }
            return member;
        }
    }
}
