using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.Requests;

public class UpdateProbeProgressStatusRequest
{
    public ProbeProgressStatus Status { get; set; }
    public string? Note { get; set; }
}
