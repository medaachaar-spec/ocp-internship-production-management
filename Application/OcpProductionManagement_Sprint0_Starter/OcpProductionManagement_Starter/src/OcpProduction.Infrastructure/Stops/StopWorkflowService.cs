using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Dtos;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Infrastructure.Data;

namespace OcpProduction.Infrastructure.Stops;

public sealed class StopWorkflowService : IStopWorkflowService
{
    private readonly OcpProductionDbContext _context;
    private readonly ILogger<StopWorkflowService> _logger;

    public StopWorkflowService(
        OcpProductionDbContext context,
        ILogger<StopWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<StopClosureData?> GetForClosureAsync(
        long stopId,
        CancellationToken cancellationToken = default)
    {
        return _context.TruckStops
            .AsNoTracking()
            .Where(stop => stop.TruckStopID == stopId)
            .Select(stop => new StopClosureData
            {
                StopId = stop.TruckStopID,
                TripId = stop.TripID,
                TruckCode = stop.Truck.TruckCode,
                StopReasonCode = stop.StopReason.ReasonCode,
                StopReasonName = stop.StopReason.ReasonName,
                DriverCode = stop.Driver == null ? null : stop.Driver.DriverCode,
                DriverName = stop.Driver == null ? null : stop.Driver.FullName,
                WorkShiftName = stop.Shift == null ? null : stop.Shift.ShiftName,
                StartTime = stop.StartTime,
                EndTime = stop.EndTime,
                ValidationStatus = stop.ValidationStatus,
                Observation = stop.Comments
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StopWorkflowResult> CloseAsync(
        CloseStopCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var stop = await _context.TruckStops.SingleOrDefaultAsync(
                candidate => candidate.TruckStopID == command.StopId,
                cancellationToken);

            if (stop is null)
            {
                return StopWorkflowResult.NotFound(command.StopId);
            }

            if (!StopWorkflowRules.CanClose(stop.EndTime, stop.ValidationStatus))
            {
                return StopWorkflowResult.InvalidState(
                    command.StopId,
                    "Cet arrêt ne peut plus être clôturé dans son état actuel.");
            }

            var errors = ValidateClosure(stop.StartTime, command.EndTime);
            if (errors.Count > 0)
            {
                return StopWorkflowResult.ValidationFailed(command.StopId, errors);
            }

            stop.EndTime = command.EndTime;
            await _context.SaveChangesAsync(cancellationToken);
            return StopWorkflowResult.Succeeded(stop.TruckStopID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "A truck stop closure encountered a concurrency conflict for stop {StopId}.",
                command.StopId);
            return StopWorkflowResult.ConcurrencyConflict(
                command.StopId,
                "Cet arrêt a déjà été traité ou son état a changé.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to close truck stop {StopId} due to a database update error.",
                command.StopId);
            return StopWorkflowResult.PersistenceFailed(
                command.StopId,
                "L’arrêt n’a pas pu être clôturé. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected error while closing truck stop {StopId}.",
                command.StopId);
            return StopWorkflowResult.PersistenceFailed(
                command.StopId,
                "Une erreur technique a empêché la clôture de l’arrêt.");
        }
    }

    public async Task<PagedResult<PendingStopListItemDto>> GetPendingValidationAsync(
        StopValidationSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        var query = _context.TruckStops
            .AsNoTracking()
            .Where(stop =>
                stop.EndTime != null &&
                stop.ValidationStatus == StopValidationStatuses.Pending);

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            var hasIdentifier = long.TryParse(search, out var identifier);
            query = query.Where(stop =>
                (hasIdentifier &&
                    (stop.TruckStopID == identifier || stop.TripID == identifier)) ||
                EF.Functions.Like(stop.Truck.TruckCode, pattern, "\\") ||
                EF.Functions.Like(stop.StopReason.ReasonCode, pattern, "\\") ||
                EF.Functions.Like(stop.StopReason.ReasonName, pattern, "\\") ||
                (stop.Comments != null && EF.Functions.Like(stop.Comments, pattern, "\\")));
        }

        if (normalized.DateFrom is { } dateFrom)
        {
            var lowerBound = dateFrom.ToDateTime(TimeOnly.MinValue);
            query = query.Where(stop => stop.EndTime >= lowerBound);
        }

        if (normalized.DateTo is { } dateTo)
        {
            if (dateTo == DateOnly.MaxValue)
            {
                query = query.Where(stop => stop.EndTime <= DateTime.MaxValue);
            }
            else
            {
                var exclusiveUpperBound = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(stop => stop.EndTime < exclusiveUpperBound);
            }
        }

        if (normalized.TruckId is { } truckId)
        {
            query = query.Where(stop => stop.TruckID == truckId);
        }

        if (normalized.StopReasonId is { } stopReasonId)
        {
            query = query.Where(stop => stop.StopReasonID == stopReasonId);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderBy(stop => stop.EndTime)
            .ThenBy(stop => stop.TruckStopID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(stop => new PendingStopListItemDto
            {
                StopId = stop.TruckStopID,
                TripId = stop.TripID,
                TruckId = stop.TruckID,
                TruckCode = stop.Truck.TruckCode,
                StopReasonId = stop.StopReasonID,
                StopReasonCode = stop.StopReason.ReasonCode,
                StopReasonName = stop.StopReason.ReasonName,
                StartTime = stop.StartTime,
                EndTime = stop.EndTime!.Value,
                DurationMinutes = stop.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PendingStopListItemDto>.Create(
            items,
            page,
            normalized.PageSize,
            totalItems);
    }

    public Task<long> GetPendingValidationCountAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.TruckStops
            .AsNoTracking()
            .LongCountAsync(
                stop =>
                    stop.EndTime != null &&
                    stop.ValidationStatus == StopValidationStatuses.Pending,
                cancellationToken);
    }

    public async Task<StopWorkflowResult> ValidateAsync(
        long stopId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stop = await _context.TruckStops.SingleOrDefaultAsync(
                candidate => candidate.TruckStopID == stopId,
                cancellationToken);

            if (stop is null)
            {
                return StopWorkflowResult.NotFound(stopId);
            }

            if (!StopWorkflowRules.CanValidate(stop.EndTime, stop.ValidationStatus))
            {
                return StopWorkflowResult.InvalidState(
                    stopId,
                    "Cet arrêt a déjà été traité ou son état a changé.");
            }

            stop.ValidationStatus = StopValidationStatuses.Validated;
            await _context.SaveChangesAsync(cancellationToken);
            return StopWorkflowResult.Succeeded(stop.TruckStopID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Truck stop validation encountered a concurrency conflict for stop {StopId}.",
                stopId);
            return StopWorkflowResult.ConcurrencyConflict(
                stopId,
                "Cet arrêt a déjà été traité ou son état a changé.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to validate truck stop {StopId} due to a database update error.",
                stopId);
            return StopWorkflowResult.PersistenceFailed(
                stopId,
                "L’arrêt n’a pas pu être validé. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected error while validating truck stop {StopId}.",
                stopId);
            return StopWorkflowResult.PersistenceFailed(
                stopId,
                "Une erreur technique a empêché la validation de l’arrêt.");
        }
    }

    private static Dictionary<string, string> ValidateClosure(
        DateTime startTime,
        DateTime endTime)
    {
        var errors = new Dictionary<string, string>();
        if (endTime == default)
        {
            errors[nameof(CloseStopCommand.EndTime)] = "L’heure de fin est obligatoire.";
        }
        else if (endTime < startTime)
        {
            errors[nameof(CloseStopCommand.EndTime)] =
                "L’heure de fin ne peut pas précéder l’heure de début.";
        }

        return errors;
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
