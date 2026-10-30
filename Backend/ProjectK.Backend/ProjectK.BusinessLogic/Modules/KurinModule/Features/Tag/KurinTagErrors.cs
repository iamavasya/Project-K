namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Tag;

/// <summary>The codes a tag request fails with; the client picks its wording by them.</summary>
public static class KurinTagErrors
{
    public const string KurinNotFound = "KurinNotFound";
    public const string TagNotFound = "TagNotFound";

    /// <summary>The kurin has tags off: nothing about them may be changed until they are on.</summary>
    public const string TagsDisabled = "TagsDisabled";

    public const string TagNameTaken = "TagNameTaken";
    public const string TagColorNotInPalette = "TagColorNotInPalette";

    /// <summary>The person would carry more than <c>KurinTagRules.MaxTagsPerMember</c>.</summary>
    public const string TooManyTags = "TooManyTags";

    /// <summary>A person named is not in the kurin now.</summary>
    public const string NotInKurin = "NotInKurin";
}
