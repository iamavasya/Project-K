using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment
{
    public record RevokeMentorCommand(Guid MentorUserKey, Guid GroupKey) : IRequest<ServiceResult<bool>>;
}
