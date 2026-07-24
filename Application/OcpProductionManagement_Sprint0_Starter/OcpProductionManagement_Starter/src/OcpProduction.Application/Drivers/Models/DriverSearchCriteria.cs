namespace OcpProduction.Application.Drivers.Models;

public sealed class DriverSearchCriteria
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;

    public DriverSearchCriteria Normalize() => new()
    {
        Search = NormalizeText(Search),
        Status = DriverStatusDefinitions.All.Contains(Status, StringComparer.Ordinal) ? Status : null,
        Page = Page > 0 ? Page : DefaultPage,
        PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize
    };

    public DriverSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : DefaultPage;
        return normalized;
    }

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
