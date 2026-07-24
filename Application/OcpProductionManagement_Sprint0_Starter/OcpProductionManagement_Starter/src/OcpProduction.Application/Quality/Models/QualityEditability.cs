namespace OcpProduction.Application.Quality.Models;

public static class QualityEditability
{
    // The verified Status values are analytical classifications, not lifecycle states.
    // Until a database-backed validation lifecycle exists, recorded measurements remain correctable.
    public static bool IsEditable(string? status) => true;
}
