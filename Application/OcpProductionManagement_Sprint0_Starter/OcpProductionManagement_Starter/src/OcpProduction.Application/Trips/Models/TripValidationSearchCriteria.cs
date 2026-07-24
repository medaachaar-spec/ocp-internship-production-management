namespace OcpProduction.Application.Trips.Models;

public sealed class TripValidationSearchCriteria
{
    public string? Search { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int Page { get; set; } = TripSearchCriteria.DefaultPage;

    public int PageSize { get; set; } = TripSearchCriteria.DefaultPageSize;

    public TripValidationSearchCriteria Normalize()
    {
        return new TripValidationSearchCriteria
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : string.Join(' ', Search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            DateFrom = DateFrom,
            DateTo = DateTo,
            Page = Page > 0 ? Page : TripSearchCriteria.DefaultPage,
            PageSize = TripSearchCriteria.AllowedPageSizes.Contains(PageSize)
                ? PageSize
                : TripSearchCriteria.DefaultPageSize
        };
    }

    public TripValidationSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : TripSearchCriteria.DefaultPage;
        return normalized;
    }
}
