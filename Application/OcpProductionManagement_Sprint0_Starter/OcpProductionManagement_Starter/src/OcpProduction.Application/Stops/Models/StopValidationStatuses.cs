namespace OcpProduction.Application.Stops.Models;

public static class StopValidationStatuses
{
    public const string Pending = "Pending";
    public const string Validated = "Validated";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Validated, Rejected];

    public static string? Normalize(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return All.FirstOrDefault(status =>
            string.Equals(status, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
