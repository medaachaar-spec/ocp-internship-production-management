using OcpProduction.Application.ReferenceData.Dtos;
using OcpProduction.Application.ReferenceData.Models;

namespace OcpProduction.Web.ViewModels.ReferenceData;

public sealed class ReferenceCatalogViewModel
{
    public IReadOnlyList<ReferenceCatalogItemDto> Items { get; init; } = [];
}

public sealed class WorkShiftListViewModel
{
    public IReadOnlyList<WorkShiftDto> Items { get; init; } = [];
}

public sealed class WorkShiftFormViewModel
{
    public int? Id { get; init; }
    public string? ImmutableCode { get; init; }
    public bool IsEdit { get; init; }
    public WorkShiftInputModel Input { get; init; } = new();
}

public sealed class LoadingPointListViewModel
{
    public IReadOnlyList<LoadingPointDto> Items { get; init; } = [];
}

public sealed class LoadingPointFormViewModel
{
    public int? Id { get; init; }
    public string? ImmutableCode { get; init; }
    public bool IsEdit { get; init; }
    public LoadingPointInputModel Input { get; init; } = new();
}

public sealed class DestinationListViewModel
{
    public IReadOnlyList<DestinationDto> Items { get; init; } = [];
    public IReadOnlyList<string> Types { get; init; } = DestinationTypes.All;
}

public sealed class DestinationFormViewModel
{
    public int? Id { get; init; }
    public string? ImmutableCode { get; init; }
    public bool IsEdit { get; init; }
    public DestinationInputModel Input { get; init; } = new();
    public IReadOnlyList<string> Types { get; init; } = DestinationTypes.All;
}

public sealed class StopReasonListViewModel
{
    public IReadOnlyList<StopReasonDto> Items { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = StopReasonCategories.All;
}

public sealed class StopReasonFormViewModel
{
    public int? Id { get; init; }
    public string? ImmutableCode { get; init; }
    public bool IsEdit { get; init; }
    public StopReasonInputModel Input { get; init; } = new();
    public IReadOnlyList<string> Categories { get; init; } = StopReasonCategories.All;
}
