using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record DeleteUserCommand(Guid UserId) : IRequest<ServiceResult<bool>>;
}
