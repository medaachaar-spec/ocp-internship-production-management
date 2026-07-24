using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcpProduction.Application.Authentication;
using OcpProduction.Application.Security;
using OcpProduction.Infrastructure;
using Xunit;

namespace OcpProduction.Tests.Authentication;

public sealed class DemoUserAuthenticationServiceTests
{
    public static IEnumerable<object[]> SupportedRoles =>
        AppRoles.All.Select(role => new object[] { role });

    [Fact]
    public void Service_source_contains_no_embedded_account_or_password_literal()
    {
        var source = File.ReadAllText(GetSolutionFile(
            "src",
            "OcpProduction.Infrastructure",
            "Authentication",
            "DemoUserAuthenticationService.cs"));

        Assert.Contains("IOptions<DemoAuthenticationOptions>", source, StringComparison.Ordinal);
        Assert.Contains("configuredUser.Password", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(
            new Regex(@"HashPassword\s*\([^,\r\n]+,\s*""", RegexOptions.CultureInvariant),
            source);
        Assert.DoesNotMatch(
            new Regex(@"Create\s*\(\s*\d+\s*,\s*""", RegexOptions.CultureInvariant),
            source);
    }

    [Fact]
    public async Task Authentication_succeeds_with_secure_test_configuration()
    {
        var settings = CreateValidSettings(
            AppRoles.Superviseur,
            out var username,
            out var password);

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        var user = await service.AuthenticateAsync(username, password);

        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
        Assert.Equal(AppRoles.Superviseur, user.Role);
    }

    [Fact]
    public async Task Authentication_rejects_an_invalid_password()
    {
        var settings = CreateValidSettings(
            AppRoles.AgentProduction,
            out var username,
            out _);

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        var user = await service.AuthenticateAsync(username, CreateEphemeralValue());

        Assert.Null(user);
    }

    [Fact]
    public async Task Authentication_rejects_an_unknown_username()
    {
        var settings = CreateValidSettings(
            AppRoles.AgentQualite,
            out _,
            out var password);

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        var user = await service.AuthenticateAsync(CreateEphemeralValue(), password);

        Assert.Null(user);
    }

    [Fact]
    public void Enabled_demo_authentication_without_users_fails_safely()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:UseDemoUsers"] = bool.TrueString
        };

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>());

        Assert.DoesNotContain("Password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Disabled_demo_authentication_creates_no_fallback_account()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:UseDemoUsers"] = bool.FalseString
        };

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        var user = await service.AuthenticateAsync(
            CreateEphemeralValue(),
            CreateEphemeralValue());

        Assert.Null(user);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_or_invalid_roles_are_rejected_without_disclosing_values(
        bool omitRole)
    {
        var settings = CreateValidSettings(
            AppRoles.Superviseur,
            out var username,
            out var password);
        var invalidRole = CreateEphemeralValue();

        if (omitRole)
        {
            settings.Remove("Authentication:DemoUsers:0:Role");
        }
        else
        {
            settings["Authentication:DemoUsers:0:Role"] = invalidRole;
        }

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>());

        Assert.DoesNotContain(username, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(invalidRole, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SupportedRoles))]
    public async Task Every_existing_application_role_is_preserved(string role)
    {
        var settings = CreateValidSettings(role, out var username, out var password);

        using var provider = CreateProvider(settings);
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();

        var user = await service.AuthenticateAsync(username, password);

        Assert.NotNull(user);
        Assert.Equal(role, user.Role);
    }

    [Fact]
    public void Validation_errors_and_logs_do_not_disclose_configured_credentials()
    {
        var settings = CreateValidSettings(
            AppRoles.Superviseur,
            out var username,
            out var password);
        var invalidRole = CreateEphemeralValue();
        settings["Authentication:DemoUsers:0:Role"] = invalidRole;
        var loggerProvider = new CapturingLoggerProvider();

        using var provider = CreateProvider(settings, loggerProvider);
        using var scope = provider.CreateScope();
        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>());
        var combinedLogs = string.Join(Environment.NewLine, loggerProvider.Messages);

        foreach (var sensitiveValue in new[] { username, password, invalidRole })
        {
            Assert.DoesNotContain(
                sensitiveValue,
                exception.ToString(),
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                sensitiveValue,
                combinedLogs,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Tracked_configuration_contains_no_demo_accounts_or_passwords()
    {
        foreach (var relativePath in new[]
                 {
                     Path.Combine("src", "OcpProduction.Web", "appsettings.json"),
                     Path.Combine("src", "OcpProduction.Web", "appsettings.Development.json"),
                     Path.Combine(
                         "src",
                         "OcpProduction.Web",
                         "Properties",
                         "launchSettings.json")
                 })
        {
            var source = File.ReadAllText(GetSolutionFile(relativePath));

            Assert.DoesNotContain("\"DemoUsers\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Password\"", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Readme_uses_only_placeholder_demo_credentials_and_login_reveals_none()
    {
        var readme = File.ReadAllText(GetSolutionFile("README.md"));
        var loginView = File.ReadAllText(GetSolutionFile(
            "src",
            "OcpProduction.Web",
            "Views",
            "Account",
            "Login.cshtml"));

        Assert.Equal(
            5,
            Regex.Matches(
                readme,
                """
                user-secrets set "Authentication:DemoUsers:\d+:Username" "<[^">\r\n]+>"
                """,
                RegexOptions.CultureInvariant).Count);
        Assert.Equal(
            5,
            Regex.Matches(
                readme,
                """
                user-secrets set "Authentication:DemoUsers:\d+:Password" "<[^">\r\n]+>"
                """,
                RegexOptions.CultureInvariant).Count);
        Assert.Equal(
            5,
            Regex.Matches(
                readme,
                """
                user-secrets set "Authentication:DemoUsers:\d+:Role" "[A-Za-z]+"
                """,
                RegexOptions.CultureInvariant).Count);
        Assert.DoesNotContain("| User | Password | Role |", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("<code>", loginView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "Exemple :",
            loginView,
            StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string?> CreateValidSettings(
        string role,
        out string username,
        out string password)
    {
        username = CreateEphemeralValue();
        password = CreateEphemeralValue();

        return new Dictionary<string, string?>
        {
            ["Authentication:UseDemoUsers"] = bool.TrueString,
            ["Authentication:DemoUsers:0:Username"] = username,
            ["Authentication:DemoUsers:0:Password"] = password,
            ["Authentication:DemoUsers:0:Role"] = role
        };
    }

    private static ServiceProvider CreateProvider(
        Dictionary<string, string?> settings,
        ILoggerProvider? loggerProvider = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            if (loggerProvider is not null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddSingleton(configuration);
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static string CreateEphemeralValue() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string GetSolutionFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OcpProductionManagement.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, Path.Combine(segments));
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Enqueue(formatter(state, exception));
            }
        }
    }
}
