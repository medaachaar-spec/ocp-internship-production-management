using OcpProduction.Application.Quality.Dtos;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Trips.Dtos;

namespace OcpProduction.Web.ViewModels;

public sealed class HomeDashboardViewModel
{
    public string RoleDisplayName { get; init; } = "utilisateur";

    public bool CanManageTrips { get; init; }

    public bool CanManageStops { get; init; }

    public bool CanManageQuality { get; init; }

    public bool CanValidate { get; init; }

    public bool CanViewDashboard { get; init; }

    public bool CanAdminister { get; init; }

    public bool CanViewTechnicalDiagnostics { get; init; }

    public long? TripsTodayCount { get; init; }

    public long? OpenStopsCount { get; init; }

    public long? QualityAnalysesTodayCount { get; init; }

    public long? PendingValidationCount { get; init; }

    public long? PendingStopValidationCount { get; init; }

    public IReadOnlyList<TripListItemDto> RecentTrips { get; init; } = [];

    public IReadOnlyList<StopListItemDto> RecentOpenStops { get; init; } = [];

    public IReadOnlyList<QualityResultListItemDto> RecentQualityAnalyses { get; init; } = [];

    public bool HasOperationalSummary =>
        TripsTodayCount.HasValue ||
        OpenStopsCount.HasValue ||
        QualityAnalysesTodayCount.HasValue ||
        PendingValidationCount.HasValue ||
        PendingStopValidationCount.HasValue;

    public bool HasRecentActivity =>
        CanManageTrips ||
        CanManageStops ||
        CanManageQuality;
}
