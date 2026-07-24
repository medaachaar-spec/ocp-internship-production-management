using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Quality.Commands;
using OcpProduction.Application.Quality.Models;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Quality;

public sealed class QualityWriteService : IQualityWriteService
{
    private const int MaximumCommentsLength = 500;
    private const decimal MaximumQualityValue = 9_999_999.999m;

    private readonly OcpProductionDbContext _context;
    private readonly ILogger<QualityWriteService> _logger;

    public QualityWriteService(
        OcpProductionDbContext context,
        ILogger<QualityWriteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QualityWriteResult> CreateAsync(
        CreateQualityResultCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var errors = ValidateCommonFields(
                command.AnalysisDate,
                command.TripId,
                command.LoadingPointId,
                command.QualityValue,
                command.Comments);

            await ValidateReferencesAsync(
                command.TripId,
                command.LoadingPointId,
                errors,
                cancellationToken);
            if (errors.Count > 0)
            {
                return QualityWriteResult.ValidationFailed(errors);
            }

            var result = new QualityResult
            {
                AnalysisDate = command.AnalysisDate,
                TripID = command.TripId,
                LoadingPointID = command.LoadingPointId,
                QualityValue = command.QualityValue,
                Comments = NormalizeComments(command.Comments)
            };

            await _context.QualityResults.AddAsync(result, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return QualityWriteResult.Succeeded(result.QualityResultID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Unable to create a quality result due to a database update error.");
            return QualityWriteResult.PersistenceFailed(
                "Le résultat qualité n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a quality result.");
            return QualityWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement du résultat qualité.");
        }
    }

    public Task<QualityEditData?> GetForEditAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _context.QualityResults
            .AsNoTracking()
            .Where(result => result.QualityResultID == id)
            .Select(result => new QualityEditData
            {
                QualityResultId = result.QualityResultID,
                AnalysisDate = result.AnalysisDate,
                TripId = result.TripID,
                LoadingPointId = result.LoadingPointID,
                QualityValue = result.QualityValue,
                Status = result.Status,
                Comments = result.Comments
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<QualityWriteResult> UpdateAsync(
        UpdateQualityResultCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var result = await _context.QualityResults.SingleOrDefaultAsync(
                candidate => candidate.QualityResultID == command.QualityResultId,
                cancellationToken);

            if (result is null)
            {
                return QualityWriteResult.NotFound(command.QualityResultId);
            }

            if (!QualityEditability.IsEditable(result.Status))
            {
                return QualityWriteResult.InvalidState(
                    command.QualityResultId,
                    "Ce résultat qualité n’est plus modifiable dans son état actuel.");
            }

            var errors = ValidateCommonFields(
                command.AnalysisDate,
                command.TripId,
                command.LoadingPointId,
                command.QualityValue,
                command.Comments);

            await ValidateReferencesAsync(
                command.TripId,
                command.LoadingPointId,
                errors,
                cancellationToken);
            if (errors.Count > 0)
            {
                return QualityWriteResult.ValidationFailed(errors);
            }

            result.AnalysisDate = command.AnalysisDate;
            result.TripID = command.TripId;
            result.LoadingPointID = command.LoadingPointId;
            result.QualityValue = command.QualityValue;
            result.Comments = NormalizeComments(command.Comments);

            await _context.SaveChangesAsync(cancellationToken);
            return QualityWriteResult.Succeeded(result.QualityResultID);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "A quality result update encountered a concurrency conflict for result {QualityResultId}.",
                command.QualityResultId);
            return QualityWriteResult.ConcurrencyConflict(
                command.QualityResultId,
                "Le résultat qualité a été modifié ou supprimé pendant votre saisie. Rechargez la page avant de réessayer.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Unable to update quality result {QualityResultId} due to a database update error.",
                command.QualityResultId);
            return QualityWriteResult.PersistenceFailed(
                "Le résultat qualité n’a pas pu être enregistré. Réessayez ou contactez le support technique.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected error while updating quality result {QualityResultId}.",
                command.QualityResultId);
            return QualityWriteResult.PersistenceFailed(
                "Une erreur technique a empêché l’enregistrement du résultat qualité.");
        }
    }

    public async Task<QualityFormOptions> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var loadingPoints = await _context.LoadingPoints
            .AsNoTracking()
            .Where(point => point.IsActive)
            .OrderBy(point => point.LoadingPointCode)
            .Select(point => new QualityLoadingPointOption(
                point.LoadingPointID,
                point.LoadingPointCode + " - " + point.LoadingPointName))
            .ToListAsync(cancellationToken);

        return new QualityFormOptions { LoadingPoints = loadingPoints };
    }

    private async Task ValidateReferencesAsync(
        long? tripId,
        int? loadingPointId,
        IDictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        int? tripLoadingPointId = null;

        if (tripId is >= 0)
        {
            tripLoadingPointId = await _context.Trips
                .AsNoTracking()
                .Where(trip => trip.TripID == tripId.Value)
                .Select(trip => (int?)trip.LoadingPointID)
                .SingleOrDefaultAsync(cancellationToken);

            if (!tripLoadingPointId.HasValue)
            {
                errors[nameof(CreateQualityResultCommand.TripId)] = "Le voyage indiqué n’existe pas.";
            }
        }

        if (loadingPointId is > 0 && !await _context.LoadingPoints
                .AsNoTracking()
                .AnyAsync(point => point.LoadingPointID == loadingPointId.Value, cancellationToken))
        {
            errors[nameof(CreateQualityResultCommand.LoadingPointId)] =
                "Le point de chargement indiqué n’existe pas.";
        }

        if (tripLoadingPointId.HasValue &&
            loadingPointId.HasValue &&
            tripLoadingPointId.Value != loadingPointId.Value)
        {
            errors[nameof(CreateQualityResultCommand.LoadingPointId)] =
                "Le point de chargement doit correspondre au voyage indiqué.";
        }
    }

    private static Dictionary<string, string> ValidateCommonFields(
        DateTime analysisDate,
        long? tripId,
        int? loadingPointId,
        decimal qualityValue,
        string? comments)
    {
        var errors = new Dictionary<string, string>();

        if (analysisDate == default)
        {
            errors[nameof(CreateQualityResultCommand.AnalysisDate)] =
                "La date et l’heure d’analyse sont obligatoires.";
        }

        if (tripId is < 0)
        {
            errors[nameof(CreateQualityResultCommand.TripId)] =
                "L’identifiant du voyage est invalide.";
        }

        if (loadingPointId is <= 0)
        {
            errors[nameof(CreateQualityResultCommand.LoadingPointId)] =
                "Le point de chargement sélectionné est invalide.";
        }

        if (!tripId.HasValue && !loadingPointId.HasValue)
        {
            errors[nameof(CreateQualityResultCommand.LoadingPointId)] =
                "Indiquez un voyage ou un point de chargement.";
        }

        if (!IsCompatibleQualityValue(qualityValue))
        {
            errors[nameof(CreateQualityResultCommand.QualityValue)] =
                "La valeur P₂O₅ doit être positive ou nulle, comporter au maximum trois décimales et respecter le format decimal(10,3).";
        }

        if (comments?.Trim().Length > MaximumCommentsLength)
        {
            errors[nameof(CreateQualityResultCommand.Comments)] =
                "L’observation ne peut pas dépasser 500 caractères.";
        }

        return errors;
    }

    private static bool IsCompatibleQualityValue(decimal value) =>
        value >= 0 &&
        value <= MaximumQualityValue &&
        decimal.Round(value, 3, MidpointRounding.ToEven) == value;

    private static string? NormalizeComments(string? comments) =>
        string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
}
