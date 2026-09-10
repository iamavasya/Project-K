using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Registry.Export
{
    /// <summary>
    /// The kurin's roster as a spreadsheet, in the columns the caller asked for and in their order.
    /// The screen decides what is shown; this only writes it down.
    /// </summary>
    /// <param name="ColumnIds">
    /// Column ids from the registry screen. Unknown ids are dropped rather than failing the export —
    /// a column removed from the app should not break a saved choice.
    /// </param>
    public sealed record ExportRegistry(Guid KurinKey, IReadOnlyList<string> ColumnIds)
        : IRequest<ServiceResult<RegistryFile>>;

    /// <summary>The file and the name to offer it under.</summary>
    public sealed record RegistryFile(byte[] Content, string FileName);
}
