namespace OcpProduction.Application.Diagnostics;

public sealed record DatabaseHealthResult(
    bool IsConnected,
    string Message,
    string? Server = null,
    string? Database = null);
