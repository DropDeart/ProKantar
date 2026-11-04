using BaseForge.Core.Exceptions;
using Tracking.Data.Pings;
using Tracking.Domain;
using Tracking.Features.DriverApp;

namespace Tracking.UnitTests;

public class PingRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static PingInput Ping(double lat = 41, double lon = 29, DateTimeOffset? at = null)
        => new(lat, lon, 10, null, null, at ?? Now);

    [Fact]
    public void Validator_AcceptsValidBatch()
        => PingBatchValidator.Validate([Ping(), Ping(at: Now.AddDays(-6))], 100, Now);

    [Theory]
    [InlineData(91, 29)]
    [InlineData(41, 181)]
    public void Validator_RejectsOutOfRangeCoordinates(double lat, double lon)
        => Assert.Throws<ValidationException>(() => PingBatchValidator.Validate([Ping(lat, lon)], 100, Now));

    [Fact]
    public void Validator_RejectsEmptyAndOversizedBatch()
    {
        Assert.Throws<ValidationException>(() => PingBatchValidator.Validate([], 100, Now));
        Assert.Throws<ValidationException>(() => PingBatchValidator.Validate([Ping(), Ping()], 1, Now));
    }

    [Fact]
    public void Validator_RejectsFutureAndTooOld()
    {
        Assert.Throws<ValidationException>(() => PingBatchValidator.Validate([Ping(at: Now.AddMinutes(10))], 100, Now));
        Assert.Throws<ValidationException>(() => PingBatchValidator.Validate([Ping(at: Now.AddDays(-8))], 100, Now));
    }

    [Fact]
    public void PartitionName_RoundTrips()
    {
        var month = PingPartitions.MonthStart(new DateTimeOffset(2026, 11, 17, 23, 0, 0, TimeSpan.FromHours(3)));

        Assert.Equal("location_pings_202611", PingPartitions.NameFor(month));
        Assert.True(PingPartitions.TryParseMonth("location_pings_202611", out var parsed));
        Assert.Equal(month, parsed);
        Assert.False(PingPartitions.TryParseMonth("location_pings_default", out _));
    }

    [Fact]
    public void GeoDistance_KnownValue()
    {
        // İstanbul (Kadıköy) – Ankara (Kızılay) kuş uçuşu ≈ 345 km.
        var meters = GeoDistance.Meters(40.9900, 29.0270, 39.9208, 32.8541);
        Assert.InRange(meters, 340_000, 350_000);
    }
}
