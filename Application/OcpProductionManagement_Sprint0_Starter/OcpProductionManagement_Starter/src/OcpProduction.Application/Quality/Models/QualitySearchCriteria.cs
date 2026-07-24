namespace OcpProduction.Application.Quality.Models;

public sealed class QualitySearchCriteria
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public string? Search { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public long? TripId { get; set; }

    public int? TruckId { get; set; }

    public decimal? P2O5Min { get; set; }

    public decimal? P2O5Max { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = DefaultPage;

    public int PageSize { get; set; } = DefaultPageSize;

    public QualitySearchCriteria Normalize()
    {
        var minimum = NormalizeMeasurement(P2O5Min);
        var maximum = NormalizeMeasurement(P2O5Max);

        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        return new QualitySearchCriteria
        {
            Search = NormalizeText(Search),
            DateFrom = DateFrom,
            DateTo = DateTo,
            TripId = TripId is >= 0 ? TripId : null,
            TruckId = TruckId is > 0 ? TruckId : null,
            P2O5Min = minimum,
            P2O5Max = maximum,
            Status = NormalizeText(Status),
            Page = Page > 0 ? Page : DefaultPage,
            PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize
        };
    }

    public QualitySearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : DefaultPage;
        return normalized;
    }

    private static decimal? NormalizeMeasurement(decimal? value) => value is >= 0 ? value : null;

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
