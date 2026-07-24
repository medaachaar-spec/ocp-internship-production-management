using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OcpProduction.Application.Diagnostics;

namespace OcpProduction.Infrastructure.Data;

public sealed class SqlServerHealthService : IDatabaseHealthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlServerHealthService> _logger;

    public SqlServerHealthService(
        IConfiguration configuration,
        ILogger<SqlServerHealthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("OcpDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DatabaseHealthResult(
                false,
                "La chaîne ConnectionStrings:OcpDatabase n'est pas configurée.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            return new DatabaseHealthResult(
                true,
                "Connexion SQL Server réussie.",
                connection.DataSource,
                connection.Database);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                "SQL Server health check failed with SQL error {SqlErrorNumber}.",
                ex.Number);

            return new DatabaseHealthResult(
                false,
                "Connexion SQL Server impossible. Consultez les journaux techniques.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "SQL Server health check failed with exception type {ExceptionType}.",
                ex.GetType().FullName);

            return new DatabaseHealthResult(
                false,
                "Connexion SQL Server impossible. Consultez les journaux techniques.");
        }
    }
}
