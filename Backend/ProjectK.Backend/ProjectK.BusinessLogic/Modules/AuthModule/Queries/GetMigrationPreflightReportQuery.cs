using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries
{
    public record GetMigrationPreflightReportQuery : IRequest<ServiceResult<MigrationPreflightReport>>;
}
