using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OcpProduction.Application.Authentication;

namespace OcpProduction.Infrastructure.Authentication;

/// <summary>
/// Local-only authentication provider for the internship prototype.
/// Credentials must be supplied through secure configuration.
/// </summary>
public sealed class DemoUserAuthenticationService : IUserAuthenticationService
{
    private readonly bool _enabled;
    private readonly PasswordHasher<DemoUser> _passwordHasher = new();
    private readonly IReadOnlyDictionary<string, DemoUser> _users;

    public DemoUserAuthenticationService(IOptions<DemoAuthenticationOptions> options)
    {
        var configuration = options.Value;
        _enabled = configuration.UseDemoUsers;
        _users = _enabled
            ? CreateUsers(configuration.DemoUsers)
            : new Dictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        if (!_users.TryGetValue(username.Trim(), out var user) || !user.IsActive)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        return Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role));
    }

    private IReadOnlyDictionary<string, DemoUser> CreateUsers(
        IReadOnlyList<DemoUserCredentialOptions> configuredUsers)
    {
        return configuredUsers
            .Select((configuredUser, index) => Create(index + 1, configuredUser))
            .ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);
    }

    private DemoUser Create(int id, DemoUserCredentialOptions configuredUser)
    {
        var username = configuredUser.Username.Trim();
        var user = new DemoUser(
            id,
            username,
            username,
            configuredUser.Role,
            string.Empty,
            true);

        return user with
        {
            PasswordHash = _passwordHasher.HashPassword(user, configuredUser.Password)
        };
    }

    private sealed record DemoUser(
        int Id,
        string Username,
        string DisplayName,
        string Role,
        string PasswordHash,
        bool IsActive);
}
