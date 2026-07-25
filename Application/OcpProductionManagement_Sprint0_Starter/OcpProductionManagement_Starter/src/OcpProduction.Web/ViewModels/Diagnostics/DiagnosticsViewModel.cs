namespace OcpProduction.Web.ViewModels.Diagnostics;

public sealed class DiagnosticsViewModel
{
    public bool ApplicationAvailable { get; init; }

    public bool DatabaseConnected { get; init; }

    public string DatabaseMessage { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string ApplicationVersion { get; init; } = string.Empty;

    public DateTimeOffset LastCheckedAt { get; init; }
}
