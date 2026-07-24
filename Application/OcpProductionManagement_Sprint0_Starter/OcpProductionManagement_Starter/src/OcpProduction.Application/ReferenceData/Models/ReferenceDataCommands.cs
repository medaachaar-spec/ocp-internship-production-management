namespace OcpProduction.Application.ReferenceData.Models;

public sealed record CreateWorkShiftCommand(
    string Code,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record UpdateWorkShiftCommand(
    int Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record CreateLoadingPointCommand(
    string Code,
    string Name,
    string? ZoneName,
    string? Description,
    bool IsActive);

public sealed record UpdateLoadingPointCommand(
    int Id,
    string Name,
    string? ZoneName,
    string? Description,
    bool IsActive);

public sealed record CreateDestinationCommand(
    string Code,
    string Name,
    string? Type,
    bool IsActive);

public sealed record UpdateDestinationCommand(
    int Id,
    string Name,
    string? Type,
    bool IsActive);

public sealed record CreateStopReasonCommand(
    string Code,
    string Name,
    string? Category,
    bool IsPlanned,
    bool IsActive);

public sealed record UpdateStopReasonCommand(
    int Id,
    string Name,
    string? Category,
    bool IsPlanned,
    bool IsActive);
