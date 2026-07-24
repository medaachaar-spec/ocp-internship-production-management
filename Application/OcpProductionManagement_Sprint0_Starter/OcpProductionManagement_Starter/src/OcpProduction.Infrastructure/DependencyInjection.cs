using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OcpProduction.Application.Authentication;
using OcpProduction.Application.Dashboard.Services;
using OcpProduction.Application.Diagnostics;
using OcpProduction.Application.Drivers.Services;
using OcpProduction.Application.Performance.Services;
using OcpProduction.Application.Quality.Services;
using OcpProduction.Application.ReferenceData.Services;
using OcpProduction.Application.Stops.Services;
using OcpProduction.Application.Trips.Services;
using OcpProduction.Application.Trucks.Services;
using OcpProduction.Infrastructure.Authentication;
using OcpProduction.Infrastructure.Dashboard;
using OcpProduction.Infrastructure.Data;
using OcpProduction.Infrastructure.Drivers;
using OcpProduction.Infrastructure.Performance;
using OcpProduction.Infrastructure.Quality;
using OcpProduction.Infrastructure.ReferenceData;
using OcpProduction.Infrastructure.Stops;
using OcpProduction.Infrastructure.Trips;
using OcpProduction.Infrastructure.Trucks;

namespace OcpProduction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OcpDatabase");

        services.AddDbContext<OcpProductionDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddSingleton<IValidateOptions<DemoAuthenticationOptions>,
            DemoAuthenticationOptionsValidator>();
        services.AddOptions<DemoAuthenticationOptions>()
            .Bind(configuration.GetSection(DemoAuthenticationOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<IUserAuthenticationService, DemoUserAuthenticationService>();
        services.AddScoped<IDashboardReadService, DashboardReadService>();
        services.AddScoped<IDatabaseHealthService, SqlServerHealthService>();
        services.AddScoped<IDriverReadService, DriverReadService>();
        services.AddScoped<IPerformanceReadService, PerformanceReadService>();
        services.AddScoped<IQualityReadService, QualityReadService>();
        services.AddScoped<IQualityWriteService, QualityWriteService>();
        services.AddScoped<ReferenceDataService>();
        services.AddScoped<IReferenceDataReadService>(provider => provider.GetRequiredService<ReferenceDataService>());
        services.AddScoped<IReferenceDataWriteService>(provider => provider.GetRequiredService<ReferenceDataService>());
        services.AddScoped<IStopReadService, StopReadService>();
        services.AddScoped<IStopWriteService, StopWriteService>();
        services.AddScoped<IStopWorkflowService, StopWorkflowService>();
        services.AddScoped<ITripReadService, TripReadService>();
        services.AddScoped<ITripWriteService, TripWriteService>();
        services.AddScoped<ITripWorkflowService, TripWorkflowService>();
        services.AddScoped<ITruckReadService, TruckReadService>();
        return services;
    }
}
