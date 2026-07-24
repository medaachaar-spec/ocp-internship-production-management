using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Stops.Commands;
using OcpProduction.Application.Stops.Models;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Stops;

public sealed class StopWriteService : IStopWriteService
{
    private const int MaximumCommentsLength = 500;

    private readonly OcpProductionDbContext _context;
    private readonly ILogger<StopWriteService> _logger;

    public StopWriteService(
        OcpProductionDbContext context,
        ILogger<StopWriteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StopWriteResult> CreateAsync(
        CreateStopCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var errors = ValidateCommonFields(
                command.TripId,
                command.TruckId,
                command.DriverId,
                command.WorkShiftId,
                command.StopReasonId,
                command.StartTime,
                command.Comments);

            await ValidateReferencesAsync(command, errors, cancellationToken);
            if (errors.Count > 0)
            {
                return StopWriteResult.ValidationFailed(errors);
            }

            var stop = new TruckStop
            {
                TripID = command.TripId,
                TruckID = command.TruckId,
                DriverID = command.DriverId,
                ShiftID = command.WorkShiftId,
                StopReasonID = command.StopReasonId,
                StartTime = command.StartTime,
                EndTime = null,
                ValidationStatus = StopValidationStatuses.Pending,
                Comments = NormalizeComments(command.Comments)
            };

            await _context.TruckStops.AddAsync(stop, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return StopWriteResult.Succeeded(stop.TruckStopID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Unable to create a truck stop due to a database update error.");
            return StopWriteResult.PersistenceFailed(
                "L’arrêt n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a truck stop.");
            return StopWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement de l’arrêt.");
        }
    }

    public Task<StopEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _context.TruckStops
            .AsNoTracking()
            .Where(stop => stop.TruckStopID == id)
            .Select(stop => new StopEditData
            {
                StopId = stop.TruckStopID,
                TripId = stop.TripID,
                TruckId = stop.TruckID,
                DriverId = stop.DriverID,
                WorkShiftId = stop.ShiftID,
                StopReasonId = stop.StopReasonID,
                StartTime = stop.StartTime,
                EndTime = stop.EndTime,
                ValidationStatus = stop.ValidationStatus,
                Comments = stop.Comments
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StopWriteResult> UpdateAsync(
        UpdateStopCommand command,
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
                return StopWriteResult.NotFound(command.StopId);
            }

            if (!StopEditability.IsEditable(stop.EndTime, stop.ValidationStatus))
            {
                return StopWriteResult.InvalidState(
                    command.StopId,
                    "Seul un arrêt ouvert et en attente de validation peut être modifié.");
            }

            var errors = ValidateCommonFields(
                command.TripId,
                command.TruckId,
                command.DriverId,
                command.WorkShiftId,
                command.StopReasonId,
                command.StartTime,
                command.Comments);

            await ValidateReferencesAsync(command, errors, cancellationToken);
            if (errors.Count > 0)
            {
                return StopWriteResult.ValidationFailed(errors);
            }

            stop.TripID = command.TripId;
            stop.TruckID = command.TruckId;
            stop.DriverID = command.DriverId;
            stop.ShiftID = command.WorkShiftId;
            stop.StopReasonID = command.StopReasonId;
            stop.StartTime = command.StartTime;
            stop.Comments = NormalizeComments(command.Comments);

            await _context.SaveChangesAsync(cancellationToken);
            return StopWriteResult.Succeeded(stop.TruckStopID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "A truck stop update encountered a concurrency conflict for stop {StopId}.",
                command.StopId);
            return StopWriteResult.ConcurrencyConflict(
                command.StopId,
                "L’arrêt a été modifié ou supprimé pendant votre saisie. Rechargez la page avant de réessayer.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to update truck stop {StopId} due to a database update error.",
                command.StopId);
            return StopWriteResult.PersistenceFailed(
                "L’arrêt n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating truck stop {StopId}.", command.StopId);
            return StopWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement de l’arrêt.");
        }
    }

    public async Task<StopFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trucks = await _context.Trucks
            .AsNoTracking()
            .Where(truck =>
                truck.IsActive &&
                (truck.Status == "Available" || truck.Status == "InService"))
            .OrderBy(truck => truck.TruckCode)
            .Select(truck => new StopFormOption(truck.TruckID, truck.TruckCode))
            .ToListAsync(cancellationToken);

        var drivers = await _context.Drivers
            .AsNoTracking()
            .Where(driver => driver.IsActive && driver.Status == "Active")
            .OrderBy(driver => driver.DriverCode)
            .Select(driver => new StopFormOption(
                driver.DriverID,
                driver.DriverCode + " - " + driver.FullName))
            .ToListAsync(cancellationToken);

        var workShifts = await _context.WorkShifts
            .AsNoTracking()
            .Where(shift => shift.IsActive)
            .OrderBy(shift => shift.ShiftCode)
            .Select(shift => new StopFormOption(
                shift.ShiftID,
                shift.ShiftCode + " - " + shift.ShiftName))
            .ToListAsync(cancellationToken);

        var stopReasons = await _context.StopReasons
            .AsNoTracking()
            .Where(reason => reason.IsActive)
            .OrderBy(reason => reason.ReasonCode)
            .Select(reason => new StopFormOption(
                reason.StopReasonID,
                reason.ReasonCode + " - " + reason.ReasonName))
            .ToListAsync(cancellationToken);

        return new StopFormOptions
        {
            Trucks = trucks,
            Drivers = drivers,
            WorkShifts = workShifts,
            StopReasons = stopReasons
        };
    }

    private async Task ValidateReferencesAsync(
        CreateStopCommand command,
        IDictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(
            command.TripId,
            command.TruckId,
            command.DriverId,
            command.WorkShiftId,
            command.StopReasonId,
            errors,
            cancellationToken);
    }

    private async Task ValidateReferencesAsync(
        UpdateStopCommand command,
        IDictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(
            command.TripId,
            command.TruckId,
            command.DriverId,
            command.WorkShiftId,
            command.StopReasonId,
            errors,
            cancellationToken);
    }

    private async Task ValidateReferencesAsync(
        long? tripId,
        int truckId,
        int? driverId,
        int? workShiftId,
        int stopReasonId,
        IDictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        if (truckId > 0 && !await _context.Trucks.AsNoTracking().AnyAsync(
                truck => truck.TruckID == truckId &&
                         truck.IsActive &&
                         (truck.Status == "Available" || truck.Status == "InService"),
                cancellationToken))
        {
            errors[nameof(CreateStopCommand.TruckId)] = "Le camion sélectionné n’est pas actif ou disponible.";
        }

        if (driverId is > 0 && !await _context.Drivers.AsNoTracking().AnyAsync(
                driver => driver.DriverID == driverId && driver.IsActive && driver.Status == "Active",
                cancellationToken))
        {
            errors[nameof(CreateStopCommand.DriverId)] = "Le conducteur sélectionné n’est pas actif.";
        }

        if (workShiftId is > 0 && !await _context.WorkShifts.AsNoTracking().AnyAsync(
                shift => shift.ShiftID == workShiftId && shift.IsActive,
                cancellationToken))
        {
            errors[nameof(CreateStopCommand.WorkShiftId)] = "Le poste sélectionné n’est pas actif.";
        }

        if (stopReasonId > 0 && !await _context.StopReasons.AsNoTracking().AnyAsync(
                reason => reason.StopReasonID == stopReasonId && reason.IsActive,
                cancellationToken))
        {
            errors[nameof(CreateStopCommand.StopReasonId)] = "Le motif d’arrêt sélectionné n’est pas actif.";
        }

        if (tripId is not > 0)
        {
            return;
        }

        var requiredTripId = tripId.GetValueOrDefault();
        var trip = await _context.Trips
            .AsNoTracking()
            .Where(candidate => candidate.TripID == requiredTripId)
            .Select(candidate => new
            {
                candidate.TruckID,
                candidate.DriverID,
                candidate.ShiftID
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (trip is null)
        {
            errors[nameof(CreateStopCommand.TripId)] = "Le voyage indiqué n’existe pas.";
            return;
        }

        if (trip.TruckID != truckId)
        {
            errors[nameof(CreateStopCommand.TruckId)] = "Le camion doit correspondre au voyage indiqué.";
        }

        if (driverId.HasValue && trip.DriverID != driverId.Value)
        {
            errors[nameof(CreateStopCommand.DriverId)] = "Le conducteur doit correspondre au voyage indiqué.";
        }

        if (workShiftId.HasValue && trip.ShiftID != workShiftId.Value)
        {
            errors[nameof(CreateStopCommand.WorkShiftId)] = "Le poste doit correspondre au voyage indiqué.";
        }
    }

    private static Dictionary<string, string> ValidateCommonFields(
        long? tripId,
        int truckId,
        int? driverId,
        int? workShiftId,
        int stopReasonId,
        DateTime startTime,
        string? comments)
    {
        var errors = new Dictionary<string, string>();

        if (tripId is <= 0)
        {
            errors[nameof(CreateStopCommand.TripId)] = "L’identifiant du voyage est invalide.";
        }

        if (truckId <= 0)
        {
            errors[nameof(CreateStopCommand.TruckId)] = "Le camion est obligatoire.";
        }

        if (driverId is <= 0)
        {
            errors[nameof(CreateStopCommand.DriverId)] = "Le conducteur sélectionné est invalide.";
        }

        if (workShiftId is <= 0)
        {
            errors[nameof(CreateStopCommand.WorkShiftId)] = "Le poste sélectionné est invalide.";
        }

        if (stopReasonId <= 0)
        {
            errors[nameof(CreateStopCommand.StopReasonId)] = "Le motif d’arrêt est obligatoire.";
        }

        if (startTime == default)
        {
            errors[nameof(CreateStopCommand.StartTime)] = "La date et l’heure de début sont obligatoires.";
        }

        if (comments?.Trim().Length > MaximumCommentsLength)
        {
            errors[nameof(CreateStopCommand.Comments)] = "L’observation ne peut pas dépasser 500 caractères.";
        }

        return errors;
    }

    private static string? NormalizeComments(string? comments) =>
        string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
}
