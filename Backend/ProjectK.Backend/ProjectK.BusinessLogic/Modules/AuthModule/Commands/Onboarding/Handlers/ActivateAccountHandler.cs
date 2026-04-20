using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers
{
    public class ActivateAccountHandler : IRequestHandler<ActivateAccountCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public ActivateAccountHandler(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<ServiceResult<Guid>> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Token
            var invitation = await _unitOfWork.Invitations.GetByTokenAsync(request.Token, cancellationToken);

            if (invitation == null || invitation.ExpiresAtUtc < DateTime.UtcNow)
            {
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, "Invalid or expired invitation token.");
            }

            // 2. Get User
            if (!invitation.TargetUserKey.HasValue)
            {
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, "Invitation is not linked to a user.");
            }

            var user = await _userManager.FindByIdAsync(invitation.TargetUserKey.Value.ToString());
            if (user == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Target user not found.");
            }

            // 3. Set Password and Activate
            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, $"Failed to set password: {errors}");
            }

            user.OnboardingStatus = OnboardingStatus.Active;
            user.EmailConfirmed = true; // Invitation activation implies email verification
            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, $"Failed to update user status: {errors}");
            }

            // 3.5. Assign Role
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(invitation.WaitlistEntryKey, cancellationToken);
            string roleToAssign = (entry?.IsKurinLeaderCandidate ?? false) 
                ? UserRole.Manager.ToString() 
                : UserRole.User.ToString();

            await _userManager.AddToRoleAsync(user, roleToAssign);

            // 4. Mark Invitation as used
            invitation.UsedAtUtc = DateTime.UtcNow;
            _unitOfWork.Invitations.Update(invitation, cancellationToken);

            // 5. Create Member record if it doesn't exist
            var existingMember = await _unitOfWork.Members.GetByEmailAsync(user.Email!, cancellationToken);
            if (existingMember == null)
            {
                var member = new Member
                {
                    MemberKey = Guid.NewGuid(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    PhoneNumber = entry?.PhoneNumber ?? "0000000000",
                    DateOfBirth = entry != null ? DateOnly.FromDateTime(entry.DateOfBirth) : new DateOnly(2000, 1, 1),
                    UserKey = user.Id,
                    KurinKey = user.KurinKey ?? Guid.Empty
                };
                _unitOfWork.Members.Create(member, cancellationToken);
            }
            else
            {
                existingMember.UserKey = user.Id;
                _unitOfWork.Members.Update(existingMember, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, user.Id);
        }
    }
}
