namespace OcpProduction.Application.ReferenceData.Dtos;

public sealed class ReferenceCatalogItemDto
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long TotalCount { get; init; }
    public long ActiveCount { get; init; }
    public string ControllerAction { get; init; } = string.Empty;
}
