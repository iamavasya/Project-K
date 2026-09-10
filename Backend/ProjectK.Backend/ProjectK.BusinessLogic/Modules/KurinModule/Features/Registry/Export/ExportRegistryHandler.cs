using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Registry.Export
{
    /// <summary>
    /// Writes the roster the caller can already see.
    /// <para>
    /// The people come from <see cref="GetMembers"/> rather than from a read of its own, and that is
    /// the point: whatever that use case decides to hide — Address and School are masked in SQL from
    /// the caller's own visibility — is hidden here too, without this handler knowing the rule. An
    /// export that read the table directly would be a way around it.
    /// </para>
    /// </summary>
    public sealed class ExportRegistryHandler : IRequestHandler<ExportRegistry, ServiceResult<RegistryFile>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISpreadsheetWriter _spreadsheet;
        private readonly TimeProvider _timeProvider;

        public ExportRegistryHandler(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            ISpreadsheetWriter spreadsheet,
            TimeProvider timeProvider)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _spreadsheet = spreadsheet;
            _timeProvider = timeProvider;
        }

        public async Task<ServiceResult<RegistryFile>> Handle(
            ExportRegistry request,
            CancellationToken cancellationToken)
        {
            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
            if (kurin is null)
            {
                return new ServiceResult<RegistryFile>(ResultType.NotFound);
            }

            var members = await _mediator.Send(new GetMembers(Guid.Empty, request.KurinKey), cancellationToken);
            if (members.Type != ResultType.Success || members.Data is null)
            {
                return new ServiceResult<RegistryFile>(members.Type);
            }

            var columns = request.ColumnIds
                .Select(id => (Id: id, Header: RegistryColumns.HeaderFor(id)))
                .Where(column => column.Header is not null)
                .ToList();

            var headers = new List<string> { "Прізвище та ім'я" };
            headers.AddRange(columns.Select(column => column.Header!));

            var people = members.Data
                .OrderBy(member => $"{member.LastName} {member.FirstName}", StringComparer.CurrentCulture)
                .ToList();

            var rows = people
                .Select(member =>
                {
                    var cells = new List<SheetCell>
                    {
                        SheetCell.Of(FullName(member))
                    };
                    cells.AddRange(columns.Select(column => RegistryColumns.Read(column.Id, member)));
                    return (IReadOnlyList<SheetCell>)cells;
                })
                .ToList();

            var content = _spreadsheet.Write($"Курінь {kurin.Number}", headers, rows);
            var stamp = _timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

            return new ServiceResult<RegistryFile>(
                ResultType.Success,
                new RegistryFile(content, $"reyestr-kurin-{kurin.Number}-{stamp}.xlsx"));
        }

        private static string FullName(Models.MemberResponse member)
            => string.Join(' ', new[] { member.LastName, member.FirstName, member.MiddleName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
