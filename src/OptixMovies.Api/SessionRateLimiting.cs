using System.Globalization;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.DataProtection;

namespace OptixMovies.Api;

/// <summary>
/// Limits API requests per browser session. A session is a signed cookie issued on the first API request, and new
/// sessions are themselves limited per IP address, so clearing cookies or forging them doesn't escape the limit.
/// </summary>
internal static class SessionRateLimiting
{
    private const string CookieName = "session";
    private static readonly object SessionKey = new();

    public static IServiceCollection AddSessionRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.Get<RateLimits>() ?? new RateLimits();

        services.AddDataProtection();
        services.AddSingleton(PartitionedRateLimiter.Create<string, string>(ipAddress =>
            RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.NewSessionsPerIpPerMinute,
                Window = TimeSpan.FromMinutes(1),
            })));

        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Items[SessionKey] is string session
                    ? RateLimitPartition.GetTokenBucketLimiter(session, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = limits.Burst,
                        TokensPerPeriod = limits.RequestsPerSecond,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    })
                    : RateLimitPartition.GetNoLimiter(string.Empty));
            options.OnRejected = (context, _) => RejectAsync(context.HttpContext, context.Lease);
        });
    }

    public static WebApplication UseSessionRateLimiting(this WebApplication app)
    {
        var protector = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("OptixMovies.Session");
        var newSessions = app.Services.GetRequiredService<PartitionedRateLimiter<string>>();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                if (ReadSession(context, protector) is { } session)
                {
                    context.Items[SessionKey] = session;
                }
                else
                {
                    using var lease = newSessions.AttemptAcquire(context.Connection.RemoteIpAddress?.ToString() ?? "");

                    if (!lease.IsAcquired)
                    {
                        await RejectAsync(context, lease);
                        return;
                    }

                    var newSession = Guid.NewGuid().ToString("N");
                    context.Response.Cookies.Append(CookieName, protector.Protect(newSession), new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = context.Request.IsHttps,
                        Path = "/api",
                    });
                    context.Items[SessionKey] = newSession;
                }
            }

            await next(context);
        });

        app.UseRateLimiter();
        return app;
    }

    private static string? ReadSession(HttpContext context, IDataProtector protector)
    {
        try
        {
            return context.Request.Cookies[CookieName] is { } cookie ? protector.Unprotect(cookie) : null;
        }
        catch (CryptographicException)
        {
            return null; // Forged, or signed with a key this server doesn't have.
        }
    }

    private static async ValueTask RejectAsync(HttpContext context, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await TypedResults.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too many requests")
            .ExecuteAsync(context);
    }

    private sealed class RateLimits
    {
        public int NewSessionsPerIpPerMinute { get; init; } = 20;
        public int RequestsPerSecond { get; init; } = 10;
        public int Burst { get; init; } = 20;
    }
}
