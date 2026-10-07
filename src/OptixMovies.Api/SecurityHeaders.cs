namespace OptixMovies.Api;

internal static class SecurityHeaders
{
    /// <summary>
    /// Adds security headers to API responses. They are only ever JSON, so browsers mustn't guess another type, render
    /// them as pages, frame them or send referrers from them. Swagger UI and the UI's dev server are left alone.
    /// </summary>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Added as the response starts, because error handling clears the headers set before it.
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers.XContentTypeOptions = "nosniff";
                    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
                    headers["Referrer-Policy"] = "no-referrer";
                    return Task.CompletedTask;
                });
            }

            return next(context);
        });

        return app;
    }
}
