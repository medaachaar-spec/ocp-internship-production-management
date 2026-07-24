namespace OcpProduction.Application.ReferenceData.Models;

public static class DestinationTypes
{
    public const string Plant = nameof(Plant);
    public const string Stockpile = nameof(Stockpile);
    public const string Waste = nameof(Waste);
    public const string Other = nameof(Other);

    public static readonly IReadOnlyList<string> All = [Plant, Stockpile, Waste, Other];
}

public static class StopReasonCategories
{
    public const string Mechanical = nameof(Mechanical);
    public const string Maintenance = nameof(Maintenance);
    public const string Operational = nameof(Operational);

    public static readonly IReadOnlyList<string> All = [Mechanical, Maintenance, Operational];
}
