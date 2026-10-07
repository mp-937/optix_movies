using Microsoft.Extensions.Diagnostics.HealthChecks;

using OptixMovies.Core;

namespace OptixMovies.Api;

/// <summary>
/// Whether the movie data can serve every feature. Without a database nothing works; with no movies, or with
/// embeddings that are out of date or from another model, the API works with less. The health endpoint reports it,
/// and the startup checks log it.
/// </summary>
internal sealed class MovieDataHealthCheck(MovieService movies, ITextEmbedder embedder) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = await movies.GetStatusAsync(cancellationToken);

        if (!status.Exists)
        {
            return HealthCheckResult.Unhealthy($"No movie database at {status.Location}. Run import.bat to build it.");
        }

        List<string> problems = [];

        if (status.MovieCount == 0)
        {
            problems.Add($"The movie database at {status.Location} has no movies.");
        }

        if (status.EmbeddingsRequired)
        {
            problems.Add("The movies need embedding; semantic search is unavailable until embed.bat runs.");
        }
        else if (status.EmbeddingModel != embedder.Model || status.EmbeddingVariant != embedder.Variant)
        {
            problems.Add(
                $"The movies were embedded with {status.EmbeddingModel} ({status.EmbeddingVariant}), but the API " +
                $"uses {embedder.Model} ({embedder.Variant}); semantic search is unavailable until embed.bat runs.");
        }

        return problems.Count == 0 ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded(string.Join(" ", problems));
    }
}
