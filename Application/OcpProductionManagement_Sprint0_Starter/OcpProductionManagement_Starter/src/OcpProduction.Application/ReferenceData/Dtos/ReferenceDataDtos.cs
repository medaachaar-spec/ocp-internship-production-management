namespace OcpProduction.Application.ReferenceData.Dtos;

public sealed class WorkShiftDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public bool IsActive { get; init; }
    public long TripUsageCount { get; init; }
    public long StopUsageCount { get; init; }
    public bool CanEdit => TripUsageCount == 0 && StopUsageCount == 0;
}

public sealed class LoadingPointDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ZoneName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public long TripUsageCount { get; init; }
    public long QualityUsageCount { get; init; }
    public bool CanEdit => TripUsageCount == 0 && QualityUsageCount == 0;
}

public sealed class DestinationDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
    public bool IsActive { get; init; }
    public long TripUsageCount { get; init; }
    public bool CanEdit => TripUsageCount == 0;
}

public sealed class StopReasonDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
    public bool IsPlanned { get; init; }
    public bool IsActive { get; init; }
    public long StopUsageCount { get; init; }
    public bool CanEdit => StopUsageCount == 0;
}
