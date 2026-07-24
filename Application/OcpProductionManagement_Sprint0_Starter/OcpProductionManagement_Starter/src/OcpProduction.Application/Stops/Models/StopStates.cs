namespace OcpProduction.Application.Stops.Models;

public static class StopStates
{
    public const string Open = "Open";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All = [Open, Closed];

    public static string? Normalize(string? value)
    {
        var normalized = NormalizeText(value);
        return All.FirstOrDefault(state =>
            string.Equals(state, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
