using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Trips;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Trips;

public sealed class TripWorkflowService : ITripWorkflowService
{
    private const decimal MaximumTonnage = 99_999_999.99m;

    private readonly OcpProductionDbContext _context;
    private readonly ILogger<TripWorkflowService> _logger;

    public TripWorkflowService(
        OcpProductionDbContext context,
        ILogger<TripWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<TripClosureData?> GetForClosureAsync(
        long tripId,
        CancellationToken cancellationToken = default)
    {
        return _context.Trips
            .AsNoTracking()
            .Where(trip => trip.TripID == tripId)
            .Select(trip => new TripClosureData
            {
                TripId = trip.TripID,
                TripDate = trip.TripDate,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                Tonnage = trip.Tonnage,
                Status = trip.Status,
                TruckCode = trip.Truck.TruckCode,
                DriverCode = trip.Driver.DriverCode,
                DriverDisplayName = trip.Driver.FullName,
                WorkShiftName = trip.Shift.ShiftName,
                LoadingPointName = trip.LoadingPoint.LoadingPointName,
                DestinationName = trip.Destination.DestinationName
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TripWorkflowResult> CloseAsync(
        CloseTripCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var trip = await _context.Trips.SingleOrDefaultAsync(
                candidate => candidate.TripID == command.TripId,
                cancellationToken);

            if (trip is null)
            {
                return TripWorkflowResult.NotFound(command.TripId);
            }

            if (!TripStatuses.CanBeClosed(trip.Status))
            {
                return TripWorkflowResult.InvalidState(
                    command.TripId,
                    "Ce voyage ne peut plus être clôturé dans son état actuel.");
            }

            var validationErrors = ValidateClosure(trip, command);
            if (validationErrors.Count > 0)
            {
                return TripWorkflowResult.ValidationFailed(command.TripId, validationErrors);
            }

            trip.EndTime = command.EndTime;
            trip.Tonnage = command.Tonnage;
            trip.Status = TripStatuses.PendingValidation;

            await _context.SaveChangesAsync(cancellationToken);
            return TripWorkflowResult.Succeeded(trip.TripID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "A trip closure encountered a concurrency conflict for trip {TripId}.",
                command.TripId);
            return TripWorkflowResult.ConcurrencyConflict(
                command.TripId,
                "Ce voyage a déjà été traité ou son état a changé.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to close trip {TripId} due to a database update error.",
                command.TripId);
            return TripWorkflowResult.PersistenceFailed(
                command.TripId,
                "Le voyage n’a pas pu être clôturé. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while closing trip {TripId}.", command.TripId);
            return TripWorkflowResult.PersistenceFailed(
                command.TripId,
                "Une erreur technique a empêché la clôture du voyage.");
        }
    }

    public async Task<PagedResult<PendingTripListItemDto>> GetPendingValidationAsync(
        TripValidationSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var normalized = criteria.Normalize();
        IQueryable<vw_TripSummary> query = _context.vw_TripSummaries
            .AsNoTracking()
            .Where(trip => trip.Status == TripStatuses.PendingValidation);

        if (normalized.Search is { } search)
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            var hasTripId = long.TryParse(search, out var tripId);
            query = query.Where(trip =>
                (hasTripId && trip.TripID == tripId) ||
                EF.Functions.Like(trip.TruckCode, pattern, "\\") ||
                EF.Functions.Like(trip.DriverCode, pattern, "\\") ||
                EF.Functions.Like(trip.FullName, pattern, "\\") ||
                EF.Functions.Like(trip.LoadingPointName, pattern, "\\") ||
                EF.Functions.Like(trip.DestinationName, pattern, "\\"));
        }

        if (normalized.DateFrom is { } dateFrom)
        {
            query = query.Where(trip => trip.TripDate >= dateFrom);
        }

        if (normalized.DateTo is { } dateTo)
        {
            query = query.Where(trip => trip.TripDate <= dateTo);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)normalized.PageSize);
        var page = Math.Clamp(normalized.Page, 1, Math.Max(totalPages, 1));

        var items = await query
            .OrderBy(trip => trip.EndTime)
            .ThenBy(trip => trip.TripID)
            .Skip((page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(trip => new PendingTripListItemDto
            {
                TripId = trip.TripID,
                TripDate = trip.TripDate,
                TruckCode = trip.TruckCode,
                DriverCode = trip.DriverCode,
                DriverDisplayName = trip.FullName,
                WorkShiftName = trip.ShiftName,
                LoadingPointName = trip.LoadingPointName,
                DestinationName = trip.DestinationName,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                Tonnage = trip.Tonnage
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PendingTripListItemDto>.Create(
            items,
            page,
            normalized.PageSize,
            totalItems);
    }

    public Task<long> GetPendingValidationCountAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.Trips
            .AsNoTracking()
            .LongCountAsync(
                trip => trip.Status == TripStatuses.PendingValidation,
                cancellationToken);
    }

    public async Task<TripWorkflowResult> ValidateAsync(
        long tripId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trip = await _context.Trips.SingleOrDefaultAsync(
                candidate => candidate.TripID == tripId,
                cancellationToken);

            if (trip is null)
            {
                return TripWorkflowResult.NotFound(tripId);
            }

            if (!TripStatuses.CanBeValidated(trip.Status))
            {
                return TripWorkflowResult.InvalidState(
                    tripId,
                    "Ce voyage a déjà été traité ou son état a changé.");
            }

            var validationErrors = new Dictionary<string, string>();
            if (trip.EndTime is null)
            {
                validationErrors[nameof(Trip.EndTime)] = "L’heure de fin est obligatoire.";
            }

            if (trip.Tonnage is null)
            {
                validationErrors[nameof(Trip.Tonnage)] = "Le tonnage est obligatoire.";
            }
            else if (trip.Tonnage <= 0)
            {
                validationErrors[nameof(Trip.Tonnage)] = "Le tonnage doit être strictement positif.";
            }

            if (validationErrors.Count > 0)
            {
                return TripWorkflowResult.ValidationFailed(tripId, validationErrors);
            }

            trip.Status = TripStatuses.Completed;
            await _context.SaveChangesAsync(cancellationToken);
            return TripWorkflowResult.Succeeded(trip.TripID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Trip validation encountered a concurrency conflict for trip {TripId}.",
                tripId);
            return TripWorkflowResult.ConcurrencyConflict(
                tripId,
                "Ce voyage a déjà été traité ou son état a changé.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to validate trip {TripId} due to a database update error.",
                tripId);
            return TripWorkflowResult.PersistenceFailed(
                tripId,
                "Le voyage n’a pas pu être validé. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while validating trip {TripId}.", tripId);
            return TripWorkflowResult.PersistenceFailed(
                tripId,
                "Une erreur technique a empêché la validation du voyage.");
        }
    }

    private static Dictionary<string, string> ValidateClosure(
        Trip trip,
        CloseTripCommand command)
    {
        var validationErrors = new Dictionary<string, string>();

        if (command.EndTime == default)
        {
            validationErrors[nameof(CloseTripCommand.EndTime)] = "L’heure de fin est obligatoire.";
        }
        else if (command.EndTime < trip.StartTime)
        {
            validationErrors[nameof(CloseTripCommand.EndTime)] =
                "L’heure de fin ne peut pas précéder l’heure de début.";
        }

        if (command.Tonnage <= 0)
        {
            validationErrors[nameof(CloseTripCommand.Tonnage)] =
                "Le tonnage doit être strictement positif.";
        }
        else if (command.Tonnage > MaximumTonnage)
        {
            validationErrors[nameof(CloseTripCommand.Tonnage)] =
                "Le tonnage ne peut pas dépasser 99 999 999,99 tonnes.";
        }

        return validationErrors;
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
