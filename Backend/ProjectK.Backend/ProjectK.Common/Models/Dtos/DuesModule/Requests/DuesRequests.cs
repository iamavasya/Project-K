using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.DuesModule.Requests;

/// <summary>A calendar quarter as the client names it.</summary>
public sealed class QuarterRequest
{
    public int Year { get; set; }
    public int Number { get; set; }
}

/// <summary>The гурток's part of the вкладка, from a quarter on.</summary>
public sealed class SetGroupDuesRateRequest
{
    public QuarterRequest FromQuarter { get; set; } = new();
    public decimal GroupShare { get; set; }
}

/// <summary>The станиця and kurin parts of the вкладка, from a quarter on, for the whole kurin.</summary>
public sealed class SetKurinDuesRateRequest
{
    public QuarterRequest FromQuarter { get; set; } = new();
    public decimal StanytsiaFull { get; set; }
    public decimal StanytsiaReduced { get; set; }
    public decimal KurinShare { get; set; }
}

/// <summary>A пільга switched on or off for one membership, from a quarter on.</summary>
public sealed class SetDuesConcessionRequest
{
    public QuarterRequest FromQuarter { get; set; } = new();
    public bool IsConcession { get; set; }
}

/// <summary>One operation of a гурток's box, new or edited.</summary>
public sealed class UpsertDuesEntryRequest
{
    public DuesEntryKind Kind { get; set; }
    public DuesPaymentMethod Method { get; set; }
    public DuesPaymentMethod? CounterMethod { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public Guid? MembershipKey { get; set; }
    public Guid? CollectedByMemberKey { get; set; }
    public string? Note { get; set; }
}

public sealed class SetDuesEntryVerifiedRequest
{
    public bool IsVerified { get; set; }
}
