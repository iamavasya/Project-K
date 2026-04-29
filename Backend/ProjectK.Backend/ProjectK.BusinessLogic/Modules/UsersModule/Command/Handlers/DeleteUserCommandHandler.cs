using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserCommandHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return new ServiceResult<bool>(ResultType.NotFound, false, "User not found.");
            }

            // Clean up MentorAssignments
            var assignments = await _unitOfWork.MentorAssignments
                .GetAllAsync(cancellationToken);
            
            var userAssignments = assignments.Where(a => a.MentorUserKey == request.UserId).ToList();

            if (userAssignments.Any())
            {
                foreach (var assignment in userAssignments)
                {
                    _unitOfWork.MentorAssignments.Delete(assignment, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Hard delete the user
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ServiceResult<bool>(ResultType.InternalServerError, false, $"Failed to delete user: {errors}");
            }

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
