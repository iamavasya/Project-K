namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

/// <summary>The rendered звіт куреня and the name it is downloaded under.</summary>
public sealed record KurinReportPdf(byte[] Content, string FileName);
