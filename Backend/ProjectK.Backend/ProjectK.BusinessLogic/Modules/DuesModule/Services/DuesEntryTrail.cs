using System.Text.Json;
using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// Writes the trail an operation leaves: one event per change, with the operation as it stands
/// afterwards. Static on purpose — it has no dependencies, and every dues handler calls it.
/// </summary>
public static class DuesEntryTrail
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Verified = "Verified";
    public const string Unverified = "Unverified";
    public const string Received = "Received";
    public const string Unreceived = "Unreceived";

    public static void Record(DuesEntry entry, string action, Guid? actorUserKey, DateTime nowUtc)
    {
        entry.UpdatedDate = nowUtc;
        entry.Events.Add(new DuesEntryEvent
        {
            DuesEntryKey = entry.DuesEntryKey,
            Action = action,
            ActorUserKey = actorUserKey,
            OccurredAtUtc = nowUtc,
            Snapshot = JsonSerializer.Serialize(new
            {
                entry.Kind,
                entry.Method,
                entry.CounterMethod,
                entry.Amount,
                entry.OccurredOn,
                entry.MembershipKey,
                entry.CollectedByMemberKey,
                entry.Note,
                entry.VerifiedAtUtc,
                entry.VerifiedByUserKey,
                entry.ReceivedAtUtc,
                entry.ReceivedByUserKey,
                entry.DeletedAtUtc,
                entry.DeletedByUserKey
            })
        });
    }
}
