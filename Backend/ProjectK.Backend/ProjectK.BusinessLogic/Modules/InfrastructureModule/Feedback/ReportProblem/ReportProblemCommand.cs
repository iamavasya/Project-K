using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Feedback.ReportProblem;

/// <summary>
/// «Повідомити про проблему» from the sidebar. The text fields arrive as Markdown the client
/// composed from its editor; <paramref name="ScreenshotUrls"/> are what
/// <c>UploadFeedbackScreenshotCommand</c> handed back, nothing else is accepted.
/// </summary>
public sealed record ReportProblemCommand(
    string? Title,
    string Description,
    string? Steps,
    string? Expected,
    string? Route,
    string? AppVersion,
    string? UserAgent,
    IReadOnlyCollection<string>? ScreenshotUrls) : IRequest<ServiceResult<ProblemReportReceipt>>;
