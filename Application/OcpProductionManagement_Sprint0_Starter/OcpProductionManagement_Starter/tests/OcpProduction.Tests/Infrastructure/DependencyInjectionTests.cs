using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcpProduction.Application.Authentication;
using OcpProduction.Application.Dashboard.Services;
using OcpProduction.Application.Diagnostics;
using OcpProduction.Application.Drivers.Services;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.ReferenceData.Services;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Application.Trucks.Services;
using OcpProduction.Infrastructure;
using OcpProduction.Infrastructure.Data;
using Xunit;

namespace OcpProduction.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_registers_Sprint_0_services_and_the_SQL_Server_context()
    {
        var connectionBuilder = new SqlConnectionStringBuilder
        {
            DataSource = "test.invalid",
            InitialCatalog = "OcpProductionTest",
            IntegratedSecurity = true,
            Encrypt = false
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OcpDatabase"] = connectionBuilder.ConnectionString,
                ["Authentication:UseDemoUsers"] = bool.FalseString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDashboardReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDatabaseHealthService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStopReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStopWriteService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripWriteService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripWorkflowService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITruckReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDriverReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPerformanceReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IReferenceDataReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IReferenceDataWriteService>());

        var context = scope.ServiceProvider.GetRequiredService<OcpProductionDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);

        var registeredConnection = new SqlConnectionStringBuilder(
            context.Database.GetConnectionString());

        Assert.Equal(connectionBuilder.DataSource, registeredConnection.DataSource);
        Assert.Equal(connectionBuilder.InitialCatalog, registeredConnection.InitialCatalog);
        Assert.Equal(connectionBuilder.IntegratedSecurity, registeredConnection.IntegratedSecurity);
        Assert.Equal(connectionBuilder.Encrypt, registeredConnection.Encrypt);
    }
}
