namespace OcpProduction.Application.Authentication;

public sealed record AuthenticatedUser(
    int UserId,
    string Username,
    string DisplayName,
    string Role);
