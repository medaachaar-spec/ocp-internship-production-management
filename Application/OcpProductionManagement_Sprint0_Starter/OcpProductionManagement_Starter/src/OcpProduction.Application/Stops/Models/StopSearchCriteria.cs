namespace OcpProduction.Application.Stops.Models;

public sealed class StopSearchCriteria
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public string? Search { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int? TruckId { get; set; }

    public int? StopReasonId { get; set; }

    public string? State { get; set; }

    public string? ValidationStatus { get; set; }

    public long? TripId { get; set; }

    public int Page { get; set; } = DefaultPage;

    public int PageSize { get; set; } = DefaultPageSize;

    public StopSearchCriteria Normalize()
    {
        return new StopSearchCriteria
        {
            Search = NormalizeText(Search),
            DateFrom = DateFrom,
            DateTo = DateTo,
            TruckId = TruckId is > 0 ? TruckId : null,
            StopReasonId = StopReasonId is > 0 ? StopReasonId : null,
            State = StopStates.Normalize(State),
            ValidationStatus = StopValidationStatuses.Normalize(ValidationStatus),
            TripId = TripId is > 0 ? TripId : null,
            Page = Page > 0 ? Page : DefaultPage,
            PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize
        };
    }

    public StopSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : DefaultPage;
        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
