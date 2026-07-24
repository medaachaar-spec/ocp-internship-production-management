using OcpProduction.Application.Trips.Models;
using Xunit;

namespace OcpProduction.Tests.Trips;

public sealed class TripWriteResultTests
{
    [Fact]
    public void Success_result_contains_generated_identifier_without_error()
    {
        var result = TripWriteResult.Succeeded(42);

        Assert.True(result.Success);
        Assert.Equal(42, result.TripId);
        Assert.Equal(TripWriteErrorCode.None, result.ErrorCode);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Validation_result_keeps_safe_field_errors()
    {
        var result = TripWriteResult.ValidationFailed(
            new Dictionary<string, string> { ["TruckId"] = "Camion invalide." });

        Assert.False(result.Success);
        Assert.Equal(TripWriteErrorCode.Validation, result.ErrorCode);
        Assert.Equal("Camion invalide.", result.ValidationErrors["TruckId"]);
    }
}
