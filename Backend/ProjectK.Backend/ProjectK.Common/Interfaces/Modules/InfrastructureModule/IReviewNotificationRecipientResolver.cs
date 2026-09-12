namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IReviewNotificationRecipientResolver
{
    Task<IReadOnlyCollection<Guid>> ResolveAsync(
        Guid kurinKey,
        Guid? groupKey,
        Guid? excludedUserKey,
        CancellationToken cancellationToken = default);
}
