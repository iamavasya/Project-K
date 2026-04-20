using MediatR;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record ChangeUserRoleCommand(Guid TargetUserId, UserRole NewRole) : IRequest<ServiceResult<bool>>;
}
