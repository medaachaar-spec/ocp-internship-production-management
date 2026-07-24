using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Trips;
using OcpProduction.Application.Trips.Commands;
using OcpProduction.Application.Trips.Models;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Trips;

public sealed class TripWriteService : ITripWriteService
{
    private const decimal MaximumTonnage = 99_999_999.99m;
    private const int MaximumCommentsLength = 500;

    private readonly OcpProductionDbContext _context;
    private readonly ILogger<TripWriteService> _logger;

    public TripWriteService(
        OcpProductionDbContext context,
        ILogger<TripWriteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TripWriteResult> CreateAsync(
        CreateTripCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var validationErrors = ValidateCommonFields(
                command.TripDate,
                command.WorkShiftId,
                command.TruckId,
                command.DriverId,
                command.LoadingPointId,
                command.DestinationId,
                command.StartTime,
                null,
                null,
                command.Comments);

            await ValidateReferencesAsync(
                command.WorkShiftId,
                command.TruckId,
                command.DriverId,
                command.LoadingPointId,
                command.DestinationId,
                validationErrors,
                cancellationToken);

            if (validationErrors.Count > 0)
            {
                return TripWriteResult.ValidationFailed(validationErrors);
            }

            var trip = new Trip
            {
                TripDate = command.TripDate,
                ShiftID = command.WorkShiftId,
                TruckID = command.TruckId,
                DriverID = command.DriverId,
                LoadingPointID = command.LoadingPointId,
                DestinationID = command.DestinationId,
                StartTime = command.StartTime,
                EndTime = null,
                Tonnage = null,
                Status = TripStatuses.InProgress,
                Comments = NormalizeComments(command.Comments)
            };

            await _context.Trips.AddAsync(trip, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return TripWriteResult.Succeeded(trip.TripID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Unable to create a trip due to a database update error.");
            return TripWriteResult.PersistenceFailed(
                "Le voyage n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a trip.");
            return TripWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement du voyage.");
        }
    }

    public Task<TripEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _context.Trips
            .AsNoTracking()
            .Where(trip => trip.TripID == id)
            .Select(trip => new TripEditData
            {
                TripId = trip.TripID,
                TripDate = trip.TripDate,
                WorkShiftId = trip.ShiftID,
                TruckId = trip.TruckID,
                DriverId = trip.DriverID,
                LoadingPointId = trip.LoadingPointID,
                DestinationId = trip.DestinationID,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                Tonnage = trip.Tonnage,
                Status = trip.Status,
                Comments = trip.Comments
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TripWriteResult> UpdateAsync(
        UpdateTripCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var trip = await _context.Trips
                .SingleOrDefaultAsync(
                    candidate => candidate.TripID == command.TripId,
                    cancellationToken);

            if (trip is null)
            {
                return TripWriteResult.NotFound(command.TripId);
            }

            if (!TripStatuses.IsEditable(trip.Status))
            {
                return TripWriteResult.InvalidState(
                    command.TripId,
                    "Ce voyage n’est plus modifiable dans son état actuel.");
            }

            var validationErrors = ValidateCommonFields(
                command.TripDate,
                command.WorkShiftId,
                command.TruckId,
                command.DriverId,
                command.LoadingPointId,
                command.DestinationId,
                command.StartTime,
                command.EndTime,
                command.Tonnage,
                command.Comments);

            await ValidateReferencesAsync(
                command.WorkShiftId,
                command.TruckId,
                command.DriverId,
                command.LoadingPointId,
                command.DestinationId,
                validationErrors,
                cancellationToken);

            if (validationErrors.Count > 0)
            {
                return TripWriteResult.ValidationFailed(validationErrors);
            }

            trip.TripDate = command.TripDate;
            trip.ShiftID = command.WorkShiftId;
            trip.TruckID = command.TruckId;
            trip.DriverID = command.DriverId;
            trip.LoadingPointID = command.LoadingPointId;
            trip.DestinationID = command.DestinationId;
            trip.StartTime = command.StartTime;
            trip.EndTime = command.EndTime;
            trip.Tonnage = command.Tonnage;
            trip.Comments = NormalizeComments(command.Comments);

            await _context.SaveChangesAsync(cancellationToken);

            return TripWriteResult.Succeeded(trip.TripID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "A trip update encountered a concurrency conflict for trip {TripId}.",
                command.TripId);
            return TripWriteResult.ConcurrencyConflict(
                command.TripId,
                "Le voyage a été modifié ou supprimé pendant votre saisie. Rechargez la page avant de réessayer.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to update trip {TripId} due to a database update error.",
                command.TripId);
            return TripWriteResult.PersistenceFailed(
                "Le voyage n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected error while updating trip {TripId}.",
                command.TripId);
            return TripWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement du voyage.");
        }
    }

    public async Task<TripFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trucks = await _context.Trucks
            .AsNoTracking()
            .Where(truck =>
                truck.IsActive &&
                (truck.Status == "Available" || truck.Status == "InService"))
            .OrderBy(truck => truck.TruckCode)
            .Select(truck => new { truck.TruckID, truck.TruckCode })
            .ToListAsync(cancellationToken);

        var drivers = await _context.Drivers
            .AsNoTracking()
            .Where(driver => driver.IsActive && driver.Status == "Active")
            .OrderBy(driver => driver.DriverCode)
            .Select(driver => new { driver.DriverID, driver.DriverCode, driver.FullName })
            .ToListAsync(cancellationToken);

        var workShifts = await _context.WorkShifts
            .AsNoTracking()
            .Where(shift => shift.IsActive)
            .OrderBy(shift => shift.ShiftCode)
            .Select(shift => new { shift.ShiftID, shift.ShiftCode, shift.ShiftName })
            .ToListAsync(cancellationToken);

        var loadingPoints = await _context.LoadingPoints
            .AsNoTracking()
            .Where(point => point.IsActive)
            .OrderBy(point => point.LoadingPointCode)
            .Select(point => new
            {
                point.LoadingPointID,
                point.LoadingPointCode,
                point.LoadingPointName
            })
            .ToListAsync(cancellationToken);

        var destinations = await _context.Destinations
            .AsNoTracking()
            .Where(destination => destination.IsActive)
            .OrderBy(destination => destination.DestinationCode)
            .Select(destination => new
            {
                destination.DestinationID,
                destination.DestinationCode,
                destination.DestinationName
            })
            .ToListAsync(cancellationToken);

        return new TripFormOptions
        {
            Trucks = trucks
                .Select(truck => new TripFormOption(truck.TruckID, truck.TruckCode))
                .ToArray(),
            Drivers = drivers
                .Select(driver => new TripFormOption(
                    driver.DriverID,
                    $"{driver.DriverCode} - {driver.FullName}"))
                .ToArray(),
            WorkShifts = workShifts
                .Select(shift => new TripFormOption(
                    shift.ShiftID,
                    $"{shift.ShiftName} ({shift.ShiftCode})"))
                .ToArray(),
            LoadingPoints = loadingPoints
                .Select(point => new TripFormOption(
                    point.LoadingPointID,
                    $"{point.LoadingPointCode} - {point.LoadingPointName}"))
                .ToArray(),
            Destinations = destinations
                .Select(destination => new TripFormOption(
                    destination.DestinationID,
                    $"{destination.DestinationCode} - {destination.DestinationName}"))
                .ToArray()
        };
    }

    private async Task ValidateReferencesAsync(
        int workShiftId,
        int truckId,
        int driverId,
        int loadingPointId,
        int destinationId,
        IDictionary<string, string> validationErrors,
        CancellationToken cancellationToken)
    {
        if (truckId > 0 && !await _context.Trucks.AsNoTracking().AnyAsync(
                truck =>
                    truck.TruckID == truckId &&
                    truck.IsActive &&
                    (truck.Status == "Available" || truck.Status == "InService"),
                cancellationToken))
        {
            validationErrors[nameof(CreateTripCommand.TruckId)] =
                "Le camion sélectionné n’existe pas ou n’est pas disponible.";
        }

        if (driverId > 0 && !await _context.Drivers.AsNoTracking().AnyAsync(
                driver =>
                    driver.DriverID == driverId &&
                    driver.IsActive &&
                    driver.Status == "Active",
                cancellationToken))
        {
            validationErrors[nameof(CreateTripCommand.DriverId)] =
                "Le conducteur sélectionné n’existe pas ou n’est pas actif.";
        }

        if (workShiftId > 0 && !await _context.WorkShifts.AsNoTracking().AnyAsync(
                shift => shift.ShiftID == workShiftId && shift.IsActive,
                cancellationToken))
        {
            validationErrors[nameof(CreateTripCommand.WorkShiftId)] =
                "Le poste sélectionné n’existe pas ou n’est pas actif.";
        }

        if (loadingPointId > 0 && !await _context.LoadingPoints.AsNoTracking().AnyAsync(
                point => point.LoadingPointID == loadingPointId && point.IsActive,
                cancellationToken))
        {
            validationErrors[nameof(CreateTripCommand.LoadingPointId)] =
                "Le point de chargement sélectionné n’existe pas ou n’est pas actif.";
        }

        if (destinationId > 0 && !await _context.Destinations.AsNoTracking().AnyAsync(
                destination => destination.DestinationID == destinationId && destination.IsActive,
                cancellationToken))
        {
            validationErrors[nameof(CreateTripCommand.DestinationId)] =
                "La destination sélectionnée n’existe pas ou n’est pas active.";
        }
    }

    private static Dictionary<string, string> ValidateCommonFields(
        DateOnly tripDate,
        int workShiftId,
        int truckId,
        int driverId,
        int loadingPointId,
        int destinationId,
        DateTime startTime,
        DateTime? endTime,
        decimal? tonnage,
        string? comments)
    {
        var validationErrors = new Dictionary<string, string>();

        AddRequiredIdentifierError(validationErrors, nameof(CreateTripCommand.TruckId), truckId, "Le camion est obligatoire.");
        AddRequiredIdentifierError(validationErrors, nameof(CreateTripCommand.DriverId), driverId, "Le conducteur est obligatoire.");
        AddRequiredIdentifierError(validationErrors, nameof(CreateTripCommand.WorkShiftId), workShiftId, "Le poste est obligatoire.");
        AddRequiredIdentifierError(validationErrors, nameof(CreateTripCommand.LoadingPointId), loadingPointId, "Le point de chargement est obligatoire.");
        AddRequiredIdentifierError(validationErrors, nameof(CreateTripCommand.DestinationId), destinationId, "La destination est obligatoire.");

        if (tripDate == default)
        {
            validationErrors[nameof(CreateTripCommand.TripDate)] = "La date opérationnelle est obligatoire.";
        }

        if (startTime == default)
        {
            validationErrors[nameof(CreateTripCommand.StartTime)] =
                "La date et l’heure de début sont obligatoires.";
        }

        if (endTime < startTime)
        {
            validationErrors[nameof(UpdateTripCommand.EndTime)] =
                "La date et l’heure de fin ne peuvent pas précéder le début.";
        }

        if (tonnage is <= 0 or > MaximumTonnage)
        {
            validationErrors[nameof(UpdateTripCommand.Tonnage)] =
                "Le tonnage doit être compris entre 0,01 et 99 999 999,99 tonnes.";
        }

        if (comments?.Length > MaximumCommentsLength)
        {
            validationErrors[nameof(CreateTripCommand.Comments)] =
                "L’observation ne peut pas dépasser 500 caractères.";
        }

        return validationErrors;
    }

    private static void AddRequiredIdentifierError(
        IDictionary<string, string> validationErrors,
        string field,
        int value,
        string message)
    {
        if (value <= 0)
        {
            validationErrors[field] = message;
        }
    }

    private static string? NormalizeComments(string? comments)
    {
        return string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
    }
}
