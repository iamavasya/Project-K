namespace ProjectK.Common.Models.Enums;

/// <summary>
/// Where the money of a стаття of the вкладка goes. Stored as a number.
/// <para>
/// <see cref="Stanytsia"/> is everything the kurin hands up (станиця, край): it is collected for
/// someone else and forwarded. <see cref="Kurin"/> stays in the kurin's box.
/// </para>
/// </summary>
public enum DuesRecipient
{
    Stanytsia = 0,
    Kurin = 1
}
