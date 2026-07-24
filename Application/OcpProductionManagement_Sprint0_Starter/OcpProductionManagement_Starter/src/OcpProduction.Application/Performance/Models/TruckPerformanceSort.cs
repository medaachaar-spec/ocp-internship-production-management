namespace OcpProduction.Application.Performance.Models;

public static class TruckPerformanceSort
{
    public const string TotalTripsDesc = nameof(TotalTripsDesc);
    public const string TotalTonnageDesc = nameof(TotalTonnageDesc);
    public const string DowntimeDesc = nameof(DowntimeDesc);
    public const string CompletionRateDesc = nameof(CompletionRateDesc);
    public const string CodeAsc = nameof(CodeAsc);

    public static readonly IReadOnlyList<string> All =
    [
        TotalTripsDesc,
        TotalTonnageDesc,
        DowntimeDesc,
        CompletionRateDesc,
        CodeAsc
    ];
}
