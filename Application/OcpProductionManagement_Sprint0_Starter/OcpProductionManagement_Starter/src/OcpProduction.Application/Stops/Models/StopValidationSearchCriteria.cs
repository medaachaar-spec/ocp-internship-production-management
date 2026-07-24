namespace OcpProduction.Application.Stops.Models;

public sealed class StopValidationSearchCriteria
{
    public string? Search { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int? TruckId { get; set; }

    public int? StopReasonId { get; set; }

    public int Page { get; set; } = StopSearchCriteria.DefaultPage;

    public int PageSize { get; set; } = StopSearchCriteria.DefaultPageSize;

    public StopValidationSearchCriteria Normalize()
    {
        return new StopValidationSearchCriteria
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : string.Join(' ', Search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            DateFrom = DateFrom,
            DateTo = DateTo,
            TruckId = TruckId is > 0 ? TruckId : null,
            StopReasonId = StopReasonId is > 0 ? StopReasonId : null,
            Page = Page > 0 ? Page : StopSearchCriteria.DefaultPage,
            PageSize = StopSearchCriteria.AllowedPageSizes.Contains(PageSize)
                ? PageSize
                : StopSearchCriteria.DefaultPageSize
        };
    }

    public StopValidationSearchCriteria WithPage(int page)
    {
        var normalized = Normalize();
        normalized.Page = page > 0 ? page : StopSearchCriteria.DefaultPage;
        return normalized;
    }
}
