using Microsoft.AspNetCore.Mvc;
using Project_K.BusinessLogic.Interfaces;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;
using Project_K.Infrastructure.Interfaces;
using Project_K.BusinessLogic.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Project_K.BusinessLogic.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IKurinLevelService _kurinLevelService;
        private readonly ITeamService _teamService;
        private readonly IMemberLevelService _memberLevelService;

        public MemberService(IMemberRepository memberRepository, IMemberLevelService memberLevelService, IKurinLevelService kurinLevelService, ITeamService teamService)
        {

            _memberRepository = memberRepository;
            _memberLevelService = memberLevelService;
            _kurinLevelService = kurinLevelService;
            _teamService = teamService;
        }

        public async Task<IEnumerable<Member>> GetMembersAsync()
        {
            return await _memberRepository.GetMembersAsync();
        }

        public async Task<Member> GetMember(uint id)
        {
            return await _memberRepository.GetByIdAsync(id);
        }

        public async Task<Member?> GetMemberDetailed(uint id)
        {
            var member = await _memberRepository.GetMemberDetailed(id);
            return member;
        }

        public async Task<MemberDto?> GetDto (uint id, string userId)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null)
            {
                return null;
            }
            var dto = MapMemberToMemberDto(member, userId);
            return dto;
        }

        public async Task<List<Member?>> GetMembersDetailed()
        {
            return await _memberRepository.GetMembersDetailed();
        }

        public async Task<Member> CreateMember(MemberDto memberDto)
        {
            Member member = MapMemberDtoToMember(memberDto);
            member.KurinLevel = await _kurinLevelService.GetByIdAsync(member.KurinLevelId);
            member.Team = await _teamService.GetByIdAsync(member.TeamId);
            await _memberRepository.AddAsync(member);
            return member;
        }

        public async Task<Member> CreateMember(MemberDto memberDto, string userId) {
            var member = MapMemberDtoToMember(memberDto);

            member.KurinLevel = await _kurinLevelService.GetByIdAsync(member.KurinLevelId);
            member.Team = await _teamService.GetByIdAsync(member.TeamId);
            member.UserId = userId;
            await _memberRepository.AddAsync(member);

            await _memberLevelService.AddMemberLevel(member.Id, memberDto.SelectedLevelId);
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

            member.KurinLevel = await _kurinLevelService.GetByIdAsync(member.KurinLevelId);
            member.Team = await _teamService.GetByIdAsync(member.TeamId);

            await _memberRepository.UpdateAsync(member);

            await _memberLevelService.UpdateMemberLevel(member.Id, member.MemberLevels.FirstOrDefault(), memberDto.SelectedLevelId);
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

        public async Task<bool> IsMemberExists(uint id)
        {
            return await _memberRepository.GetByIdAsync(id) != null;
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
                    KurinLevelId = memberDto.KurinLevelId,
                    TeamId = memberDto.TeamId
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

        private MemberDto MapMemberToMemberDto(Member member, string userId)
        {
            return new MemberDto
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                MiddleName = member.MiddleName,
                Nickname = member.Nickname,
                BirthDate = member.BirthDate,
                Phone = member.Phone,
                Email = member.Email,
                Telegram = member.Telegram,
                PlastJoin = member.PlastJoin,
                Address = member.Address,
                School = member.School,
                KurinLevelId = member.KurinLevelId,
                TeamId = member.TeamId,
                SelectedLevelId = member.MemberLevels.FirstOrDefault()?.LevelId ?? 0,
                UserId = userId
            };
        }
    }
}
