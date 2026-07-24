using Microsoft.EntityFrameworkCore;
using OcpProduction.Application.ReferenceData.Dtos;
using OcpProduction.Application.ReferenceData.Models;
using OcpProduction.Application.ReferenceData.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.ReferenceData;

public sealed class ReferenceDataService(OcpProductionDbContext context) :
    IReferenceDataReadService,
    IReferenceDataWriteService
{
    public async Task<IReadOnlyList<ReferenceCatalogItemDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var workShiftTotal = await context.WorkShifts.LongCountAsync(cancellationToken);
        var loadingTotal = await context.LoadingPoints.LongCountAsync(cancellationToken);
        var destinationTotal = await context.Destinations.LongCountAsync(cancellationToken);
        var reasonTotal = await context.StopReasons.LongCountAsync(cancellationToken);

        return
        [
            new ReferenceCatalogItemDto
            {
                Key = "work-shifts",
                Title = "Postes",
                Description = "Créneaux d'exploitation utilisés par les voyages et arrêts.",
                TotalCount = workShiftTotal,
                ActiveCount = await context.WorkShifts.LongCountAsync(item => item.IsActive, cancellationToken),
                ControllerAction = "WorkShifts"
            },
            new ReferenceCatalogItemDto
            {
                Key = "loading-points",
                Title = "Points de chargement",
                Description = "Origines de chargement utilisées par la production et la qualité.",
                TotalCount = loadingTotal,
                ActiveCount = await context.LoadingPoints.LongCountAsync(item => item.IsActive, cancellationToken),
                ControllerAction = "LoadingPoints"
            },
            new ReferenceCatalogItemDto
            {
                Key = "destinations",
                Title = "Destinations",
                Description = "Destinations validées du flux de transport.",
                TotalCount = destinationTotal,
                ActiveCount = await context.Destinations.LongCountAsync(item => item.IsActive, cancellationToken),
                ControllerAction = "Destinations"
            },
            new ReferenceCatalogItemDto
            {
                Key = "stop-reasons",
                Title = "Motifs d'arrêt",
                Description = "Causes d'arrêt planifiées ou non planifiées.",
                TotalCount = reasonTotal,
                ActiveCount = await context.StopReasons.LongCountAsync(item => item.IsActive, cancellationToken),
                ControllerAction = "StopReasons"
            }
        ];
    }

    public Task<IReadOnlyList<WorkShiftDto>> GetWorkShiftsAsync(CancellationToken cancellationToken = default)
    {
        return context.WorkShifts.AsNoTracking()
            .OrderBy(item => item.ShiftCode)
            .Select(item => new WorkShiftDto
            {
                Id = item.ShiftID,
                Code = item.ShiftCode,
                Name = item.ShiftName,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount(),
                StopUsageCount = item.TruckStops.LongCount()
            })
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<WorkShiftDto>>(task => task.Result, cancellationToken);
    }

    public Task<WorkShiftDto?> GetWorkShiftAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.WorkShifts.AsNoTracking()
            .Where(item => item.ShiftID == id)
            .Select(item => new WorkShiftDto
            {
                Id = item.ShiftID,
                Code = item.ShiftCode,
                Name = item.ShiftName,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount(),
                StopUsageCount = item.TruckStops.LongCount()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<LoadingPointDto>> GetLoadingPointsAsync(CancellationToken cancellationToken = default)
    {
        return context.LoadingPoints.AsNoTracking()
            .OrderBy(item => item.LoadingPointCode)
            .Select(item => new LoadingPointDto
            {
                Id = item.LoadingPointID,
                Code = item.LoadingPointCode,
                Name = item.LoadingPointName,
                ZoneName = item.ZoneName,
                Description = item.LoadingPointDescription,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount(),
                QualityUsageCount = item.QualityResults.LongCount()
            })
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<LoadingPointDto>>(task => task.Result, cancellationToken);
    }

    public Task<LoadingPointDto?> GetLoadingPointAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.LoadingPoints.AsNoTracking()
            .Where(item => item.LoadingPointID == id)
            .Select(item => new LoadingPointDto
            {
                Id = item.LoadingPointID,
                Code = item.LoadingPointCode,
                Name = item.LoadingPointName,
                ZoneName = item.ZoneName,
                Description = item.LoadingPointDescription,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount(),
                QualityUsageCount = item.QualityResults.LongCount()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DestinationDto>> GetDestinationsAsync(CancellationToken cancellationToken = default)
    {
        return context.Destinations.AsNoTracking()
            .OrderBy(item => item.DestinationCode)
            .Select(item => new DestinationDto
            {
                Id = item.DestinationID,
                Code = item.DestinationCode,
                Name = item.DestinationName,
                Type = item.DestinationType,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount()
            })
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<DestinationDto>>(task => task.Result, cancellationToken);
    }

    public Task<DestinationDto?> GetDestinationAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.Destinations.AsNoTracking()
            .Where(item => item.DestinationID == id)
            .Select(item => new DestinationDto
            {
                Id = item.DestinationID,
                Code = item.DestinationCode,
                Name = item.DestinationName,
                Type = item.DestinationType,
                IsActive = item.IsActive,
                TripUsageCount = item.Trips.LongCount()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<StopReasonDto>> GetStopReasonsAsync(CancellationToken cancellationToken = default)
    {
        return context.StopReasons.AsNoTracking()
            .OrderBy(item => item.ReasonCode)
            .Select(item => new StopReasonDto
            {
                Id = item.StopReasonID,
                Code = item.ReasonCode,
                Name = item.ReasonName,
                Category = item.Category,
                IsPlanned = item.IsPlanned,
                IsActive = item.IsActive,
                StopUsageCount = item.TruckStops.LongCount()
            })
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<StopReasonDto>>(task => task.Result, cancellationToken);
    }

    public Task<StopReasonDto?> GetStopReasonAsync(int id, CancellationToken cancellationToken = default)
    {
        return context.StopReasons.AsNoTracking()
            .Where(item => item.StopReasonID == id)
            .Select(item => new StopReasonDto
            {
                Id = item.StopReasonID,
                Code = item.ReasonCode,
                Name = item.ReasonName,
                Category = item.Category,
                IsPlanned = item.IsPlanned,
                IsActive = item.IsActive,
                StopUsageCount = item.TruckStops.LongCount()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> CreateWorkShiftAsync(CreateWorkShiftCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeRequired(command.Code);
        if (await context.WorkShifts.AnyAsync(item => item.ShiftCode == code, cancellationToken))
        {
            return ReferenceDataWriteResult.Failure("Ce code de poste existe déjà.");
        }

        var entity = new WorkShift
        {
            ShiftCode = code,
            ShiftName = NormalizeRequired(command.Name),
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            IsActive = command.IsActive
        };
        await context.WorkShifts.AddAsync(entity, cancellationToken);
        return await SaveCreatedAsync(entity, entity => entity.ShiftID, "Le poste a été créé.", cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> UpdateWorkShiftAsync(UpdateWorkShiftCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await context.WorkShifts.Include(item => item.Trips).Include(item => item.TruckStops)
            .SingleOrDefaultAsync(item => item.ShiftID == command.Id, cancellationToken);
        if (entity is null)
        {
            return ReferenceDataWriteResult.Failure("Le poste est introuvable.");
        }

        if (entity.Trips.Count != 0 || entity.TruckStops.Count != 0)
        {
            return ReferenceDataWriteResult.Failure("Ce poste est déjà utilisé et ne peut pas être modifié.");
        }

        entity.ShiftName = NormalizeRequired(command.Name);
        entity.StartTime = command.StartTime;
        entity.EndTime = command.EndTime;
        entity.IsActive = command.IsActive;
        return await SaveUpdatedAsync(entity.ShiftID, "Le poste a été modifié.", cancellationToken);
    }

    public Task<ReferenceDataWriteResult> SetWorkShiftActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        return SetActiveAsync(context.WorkShifts, item => item.ShiftID == id, item => item.IsActive = isActive, id, cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> CreateLoadingPointAsync(CreateLoadingPointCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeRequired(command.Code);
        if (await context.LoadingPoints.AnyAsync(item => item.LoadingPointCode == code, cancellationToken))
        {
            return ReferenceDataWriteResult.Failure("Ce code de point de chargement existe déjà.");
        }

        var entity = new LoadingPoint
        {
            LoadingPointCode = code,
            LoadingPointName = NormalizeRequired(command.Name),
            ZoneName = NormalizeOptional(command.ZoneName),
            LoadingPointDescription = NormalizeOptional(command.Description),
            IsActive = command.IsActive
        };
        await context.LoadingPoints.AddAsync(entity, cancellationToken);
        return await SaveCreatedAsync(entity, entity => entity.LoadingPointID, "Le point de chargement a été créé.", cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> UpdateLoadingPointAsync(UpdateLoadingPointCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await context.LoadingPoints.Include(item => item.Trips).Include(item => item.QualityResults)
            .SingleOrDefaultAsync(item => item.LoadingPointID == command.Id, cancellationToken);
        if (entity is null)
        {
            return ReferenceDataWriteResult.Failure("Le point de chargement est introuvable.");
        }

        if (entity.Trips.Count != 0 || entity.QualityResults.Count != 0)
        {
            return ReferenceDataWriteResult.Failure("Ce point de chargement est déjà utilisé et ne peut pas être modifié.");
        }

        entity.LoadingPointName = NormalizeRequired(command.Name);
        entity.ZoneName = NormalizeOptional(command.ZoneName);
        entity.LoadingPointDescription = NormalizeOptional(command.Description);
        entity.IsActive = command.IsActive;
        return await SaveUpdatedAsync(entity.LoadingPointID, "Le point de chargement a été modifié.", cancellationToken);
    }

    public Task<ReferenceDataWriteResult> SetLoadingPointActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        return SetActiveAsync(context.LoadingPoints, item => item.LoadingPointID == id, item => item.IsActive = isActive, id, cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> CreateDestinationAsync(CreateDestinationCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeRequired(command.Code);
        var type = NormalizeOptional(command.Type);
        if (type is not null && !DestinationTypes.All.Contains(type, StringComparer.Ordinal))
        {
            return ReferenceDataWriteResult.Failure("Le type de destination est invalide.");
        }

        if (await context.Destinations.AnyAsync(item => item.DestinationCode == code, cancellationToken))
        {
            return ReferenceDataWriteResult.Failure("Ce code de destination existe déjà.");
        }

        var entity = new Destination
        {
            DestinationCode = code,
            DestinationName = NormalizeRequired(command.Name),
            DestinationType = type,
            IsActive = command.IsActive
        };
        await context.Destinations.AddAsync(entity, cancellationToken);
        return await SaveCreatedAsync(entity, entity => entity.DestinationID, "La destination a été créée.", cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> UpdateDestinationAsync(UpdateDestinationCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await context.Destinations.Include(item => item.Trips)
            .SingleOrDefaultAsync(item => item.DestinationID == command.Id, cancellationToken);
        if (entity is null)
        {
            return ReferenceDataWriteResult.Failure("La destination est introuvable.");
        }

        if (entity.Trips.Count != 0)
        {
            return ReferenceDataWriteResult.Failure("Cette destination est déjà utilisée et ne peut pas être modifiée.");
        }

        var type = NormalizeOptional(command.Type);
        if (type is not null && !DestinationTypes.All.Contains(type, StringComparer.Ordinal))
        {
            return ReferenceDataWriteResult.Failure("Le type de destination est invalide.");
        }

        entity.DestinationName = NormalizeRequired(command.Name);
        entity.DestinationType = type;
        entity.IsActive = command.IsActive;
        return await SaveUpdatedAsync(entity.DestinationID, "La destination a été modifiée.", cancellationToken);
    }

    public Task<ReferenceDataWriteResult> SetDestinationActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        return SetActiveAsync(context.Destinations, item => item.DestinationID == id, item => item.IsActive = isActive, id, cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> CreateStopReasonAsync(CreateStopReasonCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeRequired(command.Code);
        var category = NormalizeOptional(command.Category);
        if (category is not null && !StopReasonCategories.All.Contains(category, StringComparer.Ordinal))
        {
            return ReferenceDataWriteResult.Failure("La catégorie du motif d'arrêt est invalide.");
        }

        if (await context.StopReasons.AnyAsync(item => item.ReasonCode == code, cancellationToken))
        {
            return ReferenceDataWriteResult.Failure("Ce code de motif existe déjà.");
        }

        var entity = new StopReason
        {
            ReasonCode = code,
            ReasonName = NormalizeRequired(command.Name),
            Category = category,
            IsPlanned = command.IsPlanned,
            IsActive = command.IsActive
        };
        await context.StopReasons.AddAsync(entity, cancellationToken);
        return await SaveCreatedAsync(entity, entity => entity.StopReasonID, "Le motif d'arrêt a été créé.", cancellationToken);
    }

    public async Task<ReferenceDataWriteResult> UpdateStopReasonAsync(UpdateStopReasonCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await context.StopReasons.Include(item => item.TruckStops)
            .SingleOrDefaultAsync(item => item.StopReasonID == command.Id, cancellationToken);
        if (entity is null)
        {
            return ReferenceDataWriteResult.Failure("Le motif d'arrêt est introuvable.");
        }

        if (entity.TruckStops.Count != 0)
        {
            return ReferenceDataWriteResult.Failure("Ce motif d'arrêt est déjà utilisé et ne peut pas être modifié.");
        }

        var category = NormalizeOptional(command.Category);
        if (category is not null && !StopReasonCategories.All.Contains(category, StringComparer.Ordinal))
        {
            return ReferenceDataWriteResult.Failure("La catégorie du motif d'arrêt est invalide.");
        }

        entity.ReasonName = NormalizeRequired(command.Name);
        entity.Category = category;
        entity.IsPlanned = command.IsPlanned;
        entity.IsActive = command.IsActive;
        return await SaveUpdatedAsync(entity.StopReasonID, "Le motif d'arrêt a été modifié.", cancellationToken);
    }

    public Task<ReferenceDataWriteResult> SetStopReasonActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        return SetActiveAsync(context.StopReasons, item => item.StopReasonID == id, item => item.IsActive = isActive, id, cancellationToken);
    }

    private async Task<ReferenceDataWriteResult> SetActiveAsync<TEntity>(
        DbSet<TEntity> set,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> setState,
        int id,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await set.SingleOrDefaultAsync(predicate, cancellationToken);
        if (entity is null)
        {
            return ReferenceDataWriteResult.Failure("L'élément de référentiel est introuvable.");
        }

        setState(entity);
        return await SaveUpdatedAsync(id, "L'état actif a été mis à jour.", cancellationToken);
    }

    private async Task<ReferenceDataWriteResult> SaveCreatedAsync<TEntity>(
        TEntity entity,
        Func<TEntity, int> getId,
        string message,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return ReferenceDataWriteResult.Success(getId(entity), message);
        }
        catch (DbUpdateException)
        {
            return ReferenceDataWriteResult.Failure("La modification du référentiel a été refusée par SQL Server.");
        }
    }

    private async Task<ReferenceDataWriteResult> SaveUpdatedAsync(
        int id,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return ReferenceDataWriteResult.Success(id, message);
        }
        catch (DbUpdateException)
        {
            return ReferenceDataWriteResult.Failure("La modification du référentiel a été refusée par SQL Server.");
        }
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
