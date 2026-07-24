namespace OcpProduction.Application.Trips.Models;

public sealed class TripSearchCriteria
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public string? Search { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int? TruckId { get; set; }

    public int? DriverId { get; set; }

    public int? WorkShiftId { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = DefaultPage;

    public int PageSize { get; set; } = DefaultPageSize;

    public TripSearchCriteria Normalize()
    {
        return new TripSearchCriteria
        {
            Search = NormalizeText(Search),
            DateFrom = DateFrom,
            DateTo = DateTo,
            TruckId = NormalizeIdentifier(TruckId),
            DriverId = NormalizeIdentifier(DriverId),
            WorkShiftId = NormalizeIdentifier(WorkShiftId),
            Status = NormalizeText(Status),
            Page = Page > 0 ? Page : DefaultPage,
            PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize
        };
    }

    public TripSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : DefaultPage;
        return normalized;
    }

    private static int? NormalizeIdentifier(int? value) => value is > 0 ? value : null;

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
