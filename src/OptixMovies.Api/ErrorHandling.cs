using Microsoft.AspNetCore.Diagnostics;

namespace OptixMovies.Api;

/// <summary>
/// Makes every error a problem details response. Errors the API checks for say what went wrong; anything else is a
/// generic 500, so internal details never reach clients, and the exception is logged in full.
/// </summary>
internal static class ErrorHandling
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        // A missing or malformed parameter throws in every environment, not only in development, so the handler below
        // can say what's wrong.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        return services.AddProblemDetails().AddExceptionHandler<ExpectedExceptionHandler>();
    }

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(); // Gives bodiless errors, such as 404s and timeouts, a problem details body.
        return app;
    }

    /// <summary>Exceptions that clients should hear about. Add a case for each one the API comes to expect.</summary>
    private sealed class ExpectedExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            int? status = exception switch
            {
                BadHttpRequestException badRequest => badRequest.StatusCode, // A missing or malformed parameter.
                _ => null,
            };

            if (status is null)
            {
                return false;
            }

            context.Response.StatusCode = status.Value;

            return await problems.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = { Status = status, Detail = exception.Message },
            });
        }
    }
}
