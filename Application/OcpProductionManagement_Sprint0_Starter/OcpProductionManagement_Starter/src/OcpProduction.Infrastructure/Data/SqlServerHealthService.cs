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
                "La base de données n’est pas accessible. Consultez la configuration du service.");
        }

        try
        {
            var connectionOptions = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 5
            };
            await using var connection = new SqlConnection(connectionOptions.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return new DatabaseHealthResult(
                true,
                "SQL Server accessible. Base de données accessible.",
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
                "SQL Server ou base de données inaccessible. Consultez les journaux techniques.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "SQL Server health check failed with exception type {ExceptionType}.",
                ex.GetType().FullName);

            return new DatabaseHealthResult(
                false,
                "SQL Server ou base de données inaccessible. Consultez les journaux techniques.");
        }
    }
}
