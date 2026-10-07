using OptixMovies.Core;

namespace OptixMovies.Api;

internal static class StartupChecks
{
    /// <summary>
    /// Logs any problems with the movie data before the API starts serving. Returns <see langword="false"/> if there's
    /// no database, in which case the API can't serve anything.
    /// </summary>
    public static async Task<bool> CheckDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var status = await scope.ServiceProvider.GetRequiredService<MovieService>().GetStatusAsync();

        if (!status.Exists)
        {
            app.Logger.LogCritical("No movie database at {Location}. Run import.bat to build it.", status.Location);
            return false;
        }

        if (status.MovieCount == 0)
        {
            app.Logger.LogWarning("The movie database at {Location} has no movies.", status.Location);
        }

        var embedder = scope.ServiceProvider.GetRequiredService<ITextEmbedder>();

        if (status.EmbeddingsRequired)
        {
            app.Logger.LogWarning("The movies need embedding; semantic search is unavailable until embed.bat runs.");
        }
        else if (status.EmbeddingModel != embedder.Model || status.EmbeddingVariant != embedder.Variant)
        {
            app.Logger.LogWarning(
                "The movies were embedded with {StoredModel} ({StoredVariant}), but the API uses {Model} ({Variant}); " +
                "semantic search is unavailable until embed.bat runs.",
                status.EmbeddingModel,
                status.EmbeddingVariant,
                embedder.Model,
                embedder.Variant);
        }

        return true;
    }
}
