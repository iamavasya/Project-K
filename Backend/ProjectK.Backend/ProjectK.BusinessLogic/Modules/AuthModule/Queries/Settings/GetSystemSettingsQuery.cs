using MediatR;
using ProjectK.Common.Models.Records;
using System.Collections.Generic;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Settings
{
    public record GetSystemSettingsQuery() : IRequest<ServiceResult<Dictionary<string, string>>>;
}
