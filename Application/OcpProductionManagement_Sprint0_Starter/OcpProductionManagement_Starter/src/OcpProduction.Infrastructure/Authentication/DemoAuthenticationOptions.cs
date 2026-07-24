using Microsoft.Extensions.Options;
using OcpProduction.Application.Security;

namespace OcpProduction.Infrastructure.Authentication;

public sealed class DemoAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool UseDemoUsers { get; set; }

    public List<DemoUserCredentialOptions> DemoUsers { get; set; } = [];
}

public sealed class DemoUserCredentialOptions
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

internal sealed class DemoAuthenticationOptionsValidator
    : IValidateOptions<DemoAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, DemoAuthenticationOptions options)
    {
        if (!options.UseDemoUsers)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.DemoUsers.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                "Demo authentication is enabled, but no local demo users are configured.");
        }

        if (options.DemoUsers.Any(user =>
                string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.Password)))
        {
            return ValidateOptionsResult.Fail(
                "Each configured demo user requires a username and password.");
        }

        if (options.DemoUsers.Any(user =>
                string.IsNullOrWhiteSpace(user.Role) ||
                !AppRoles.All.Contains(user.Role, StringComparer.Ordinal)))
        {
            return ValidateOptionsResult.Fail(
                "Each configured demo user requires a valid application role.");
        }

        if (options.DemoUsers
            .Select(user => user.Username.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != options.DemoUsers.Count)
        {
            return ValidateOptionsResult.Fail(
                "Configured demo usernames must be unique.");
        }

        return ValidateOptionsResult.Success;
    }
}
