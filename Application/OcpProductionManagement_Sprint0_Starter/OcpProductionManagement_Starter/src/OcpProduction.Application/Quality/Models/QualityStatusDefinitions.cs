namespace OcpProduction.Application.Quality.Models;

public static class QualityStatusDefinitions
{
    public const string Pending = "Pending";
    public const string Review = "Review";
    public const string AboveTarget = "AboveTarget";
    public const string BelowTarget = "BelowTarget";
    public const string InTarget = "InTarget";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Review,
        AboveTarget,
        BelowTarget,
        InTarget
    };
}
