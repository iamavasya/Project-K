using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Handlers
{
    public class GetMigrationPreflightReportHandler : IRequestHandler<GetMigrationPreflightReportQuery, ServiceResult<MigrationPreflightReport>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetMigrationPreflightReportHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<MigrationPreflightReport>> Handle(GetMigrationPreflightReportQuery request, CancellationToken cancellationToken)
        {
            var report = new MigrationPreflightReport();

            var members = (await _unitOfWork.Members.GetAllAsync(cancellationToken)).ToList();
            var users = await _userManager.Users.ToListAsync(cancellationToken);

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
                        $"{member.FirstName} {member.LastName}",
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

            // 4. Inconsistent Links
            foreach (var member in members.Where(m => m.UserKey.HasValue))
            {
                var user = users.FirstOrDefault(u => u.Id == member.UserKey.Value);
                if (user != null)
                {
                    if (user.KurinKey != member.KurinKey)
                    {
                        report.InconsistentLinks.Add(new InconsistentLinkInfo(
                            member.MemberKey,
                            user.Id,
                            member.KurinKey,
                            user.KurinKey));
                    }
                }
            }

            return new ServiceResult<MigrationPreflightReport>(ResultType.Success, report);
        }
    }
}
