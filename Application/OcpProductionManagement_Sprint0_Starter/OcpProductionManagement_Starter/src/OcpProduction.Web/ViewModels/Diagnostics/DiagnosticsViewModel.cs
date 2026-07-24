namespace OcpProduction.Web.ViewModels.Diagnostics;

public sealed class DiagnosticsViewModel
{
    public bool DatabaseConnected { get; init; }
    public string DatabaseMessage { get; init; } = string.Empty;
    public string? Server { get; init; }
    public string? Database { get; init; }
    public string EnvironmentName { get; init; } = string.Empty;
    public string CurrentUser { get; init; } = string.Empty;
    public string CurrentRole { get; init; } = string.Empty;
}
