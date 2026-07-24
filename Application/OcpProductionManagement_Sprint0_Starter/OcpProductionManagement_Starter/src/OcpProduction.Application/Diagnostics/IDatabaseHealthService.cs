namespace OcpProduction.Application.Diagnostics;

public interface IDatabaseHealthService
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}
