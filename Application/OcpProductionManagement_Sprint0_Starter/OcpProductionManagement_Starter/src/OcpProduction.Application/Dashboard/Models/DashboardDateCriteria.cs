namespace OcpProduction.Application.Dashboard.Models;

public sealed class DashboardDateCriteria
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool HasInvalidRange =>
        StartDate is { } startDate &&
        EndDate is { } endDate &&
        startDate > endDate;
}
