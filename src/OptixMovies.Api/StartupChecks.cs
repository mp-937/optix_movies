using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OptixMovies.Api;

internal static class StartupChecks
{
    /// <summary>
    /// Runs the health checks before the API starts serving, and logs anything they find. Returns
    /// <see langword="false"/> if the API is unhealthy, as it is without a database, in which case it can't serve.
    /// </summary>
    public static async Task<bool> CheckHealthAsync(this WebApplication app)
    {
        var report = await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        foreach (var entry in report.Entries.Values)
        {
            if (entry.Status == HealthStatus.Unhealthy)
            {
                app.Logger.LogCritical(entry.Exception, "{Problem}", entry.Description);
            }
            else if (entry.Status == HealthStatus.Degraded)
            {
                app.Logger.LogWarning("{Problem}", entry.Description);
            }
        }

        return report.Status != HealthStatus.Unhealthy;
    }
}
