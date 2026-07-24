namespace OcpProduction.Application.Performance.Models;

public static class DriverPerformanceSort
{
    public const string TotalTripsDesc = nameof(TotalTripsDesc);
    public const string TotalTonnageDesc = nameof(TotalTonnageDesc);
    public const string StopDurationDesc = nameof(StopDurationDesc);
    public const string CompletionRateDesc = nameof(CompletionRateDesc);
    public const string CodeAsc = nameof(CodeAsc);

    public static readonly IReadOnlyList<string> All =
    [
        TotalTripsDesc,
        TotalTonnageDesc,
        StopDurationDesc,
        CompletionRateDesc,
        CodeAsc
    ];
}
