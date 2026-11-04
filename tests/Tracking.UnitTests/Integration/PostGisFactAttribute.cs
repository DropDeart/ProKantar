namespace Tracking.UnitTests.Integration;

/// <summary>TRACKING_TEST_DB (PostGIS bağlantı dizesi) tanımlı değilse testi atlar.</summary>
public sealed class PostGisFactAttribute : FactAttribute
{
    /// <summary>Test veritabanı bağlantı dizesi.</summary>
    public static string? ConnectionString => Environment.GetEnvironmentVariable("TRACKING_TEST_DB");

    public PostGisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Skip = "TRACKING_TEST_DB tanımlı değil (örn. tracking/docker-compose postgres).";
        }
    }
}
