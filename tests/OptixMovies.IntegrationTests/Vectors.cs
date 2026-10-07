using OptixMovies.Core;

namespace OptixMovies.IntegrationTests;

internal static class Vectors
{
    /// <summary>The cosine similarity of two vectors.</summary>
    public static double Similarity(float[] a, float[] b)
    {
        double dot = 0, lengthA = 0, lengthB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            lengthA += a[i] * a[i];
            lengthB += b[i] * b[i];
        }

        return dot / Math.Sqrt(lengthA * lengthB);
    }
}

/// <summary>Stands in for a different embedding model: every text gets the same vector.</summary>
internal sealed class OtherModelEmbedder : ITextEmbedder
{
    public string Model => "other-model";

    public string Variant => "v1";

    public int Dimensions => 384;

    public Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> documents, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<float[]>>([.. documents.Select(_ => Vector())]);

    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default) =>
        Task.FromResult(Vector());

    private float[] Vector() => [.. Enumerable.Repeat(1 / MathF.Sqrt(Dimensions), Dimensions)];
}
