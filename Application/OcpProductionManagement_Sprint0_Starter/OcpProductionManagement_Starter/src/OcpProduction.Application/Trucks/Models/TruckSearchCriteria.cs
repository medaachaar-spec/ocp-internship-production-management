namespace OcpProduction.Application.Trucks.Models;

public sealed class TruckSearchCriteria
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Model { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;

    public TruckSearchCriteria Normalize() => new()
    {
        Search = NormalizeText(Search),
        Status = TruckStatusDefinitions.All.Contains(Status, StringComparer.Ordinal) ? Status : null,
        Model = NormalizeText(Model),
        Page = Page > 0 ? Page : DefaultPage,
        PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize
    };

    public TruckSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : DefaultPage;
        return normalized;
    }

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
