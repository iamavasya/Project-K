using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Migration.PreflightReport
{
    public class GetMigrationPreflightReportHandler : IRequestHandler<GetMigrationPreflightReportQuery, ServiceResult<MigrationPreflightReport>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemberDirectory _members;
        private readonly IMembershipDirectory _memberships;

        public GetMigrationPreflightReportHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IMemberDirectory members,
            IMembershipDirectory memberships)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _members = members;
            _memberships = memberships;
        }

        public async Task<ServiceResult<MigrationPreflightReport>> Handle(GetMigrationPreflightReportQuery request, CancellationToken cancellationToken)
        {
            var report = new MigrationPreflightReport();

            var members = (await _members.GetAllAsync(cancellationToken)).ToList();
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);

            report.TotalMembers = members.Count;
            report.TotalUsers = users.Count;

            // 1. Duplicate Email Conflicts
            var memberEmails = members.GroupBy(m => m.Email.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateEmailConflict(g.Key, g.Select(m => m.MemberKey).ToList(), new List<Guid>()))
                .ToList();

            var userEmails = users.GroupBy(u => u.Email!.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateEmailConflict(g.Key, new List<Guid>(), g.Select(u => u.Id).ToList()))
                .ToList();

            report.DuplicateEmailConflicts.AddRange(memberEmails);
            // Combine with cross-table duplicates if needed, but usually email is unique in Identity

            // 2. Orphan Members
            foreach (var member in members.Where(m => m.UserKey.HasValue))
            {
                if (!users.Any(u => u.Id == member.UserKey.Value))
                {
                    report.OrphanMembers.Add(new OrphanMemberInfo(
                        member.MemberKey,
                        member.FullName,
                        member.Email,
                        member.UserKey));
                }
            }

            // 3. Orphan Users (Users not linked to any member)
            var linkedUserIds = members.Where(m => m.UserKey.HasValue).Select(m => m.UserKey!.Value).ToHashSet();
            foreach (var user in users)
            {
                if (!linkedUserIds.Contains(user.Id))
                {
                    // Check if user is Admin - maybe admins don't need members? 
                    // But for this project, every user should ideally have a member profile.
                    report.OrphanUsers.Add(new OrphanUserInfo(user.Id, user.UserName!, user.Email!));
                }
            }

            // 4. Inconsistent Links — an account whose snapshot names a kurin the person does not
            // stand in. Only a contradiction is reported, never a merely absent snapshot:
            // AppUser.KurinKey is written when the account is opened and never again, so most
            // accounts carry nothing, and reporting those would be reporting the whole table.
            var standing = await _memberships.GetCurrentForAccountsAsync(
                users.Select(user => user.Id).ToList(),
                cancellationToken);

            foreach (var member in members.Where(m => m.UserKey.HasValue))
            {
                var user = users.FirstOrDefault(u => u.Id == member.UserKey!.Value);
                if (user?.KurinKey is null || user.KurinKey == Guid.Empty)
                {
                    continue;
                }

                var kurinsHere = standing.TryGetValue(user.Id, out var current)
                    ? current.Select(record => record.KurinKey).ToList()
                    : [];

                if (kurinsHere.Count > 0 && !kurinsHere.Contains(user.KurinKey.Value))
                {
                    report.InconsistentLinks.Add(new InconsistentLinkInfo(
                        member.MemberKey,
                        user.Id,
                        kurinsHere[0],
                        user.KurinKey));
                }
            }

            return new ServiceResult<MigrationPreflightReport>(ResultType.Success, report);
        }
    }
}
