using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

using OptixMovies.Core;
using OptixMovies.Embeddings.Onnx;

namespace OptixMovies.IntegrationTests;

/// <summary>The embedding engine, ONNX Runtime, running the committed model.</summary>
public sealed class EmbeddingTests(TestData data)
{
    private static readonly string ModelFolder = Path.Combine(AppContext.BaseDirectory, "Model");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public void ModelFileIsTheOneItsReadmeDescribes()
    {
        var readme = File.ReadAllText(Path.Combine(ModelFolder, "README.md"));
        var expected = Regex.Match(readme, "SHA-256 `([0-9a-f]{64})`").Groups[1].Value;
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(ModelFolder, "model.onnx"))));

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OnnxRuntimeLoadsTheModelOnThisPlatform()
    {
        using var embedder = new OnnxTextEmbedder();

        var vectors = await embedder.EmbedDocumentsAsync(["Toy Story"], Cancellation);

        Assert.Equal(("bge-small-en-v1.5", "int8", 384), (embedder.Model, embedder.Variant, embedder.Dimensions));
        Assert.Equal(384, Assert.Single(vectors).Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Toy Story")]
    [InlineData("A narcissistic TV weatherman finds himself repeating the same day over and over.")]
    public async Task VectorsHaveLengthOne(string text)
    {
        var vector = Assert.Single(await data.Embedder.EmbedDocumentsAsync([text], Cancellation));

        Assert.Equal(data.Embedder.Dimensions, vector.Length);
        Assert.Equal(1, Math.Sqrt(vector.Sum(value => (double)value * value)), precision: 5);
    }

    [Fact]
    public async Task MatchesTheReferenceImplementation()
    {
        var reference = JsonSerializer.Deserialize<ReferenceEmbedding>(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "reference-embedding.json"), Cancellation),
            JsonSerializerOptions.Web)!;

        var vector = Assert.Single(await data.Embedder.EmbedDocumentsAsync([reference.Text], Cancellation));

        Assert.InRange(Vectors.Similarity(reference.Vector, vector), 0.9999, 1.0001);
    }

    [Fact]
    public async Task TextGetsTheSameVectorAloneOrWithOthers()
    {
        const string text = "Led by Woody, Andy's toys live happily in his room.";

        var alone = await data.Embedder.EmbedDocumentsAsync([text], Cancellation);
        var withOthers = await data.Embedder.EmbedDocumentsAsync(
            ["A shark terrorises a beach town.", text, "Batman raises the stakes in his war on crime."], Cancellation);

        Assert.InRange(Vectors.Similarity(alone[0], withOthers[1]), 0.999999, 1.000001);
    }

    [Fact]
    public async Task QueryIsEmbeddedDifferentlyFromTheSameTextAsADocument()
    {
        const string text = "toys that come alive";

        var query = await data.Embedder.EmbedQueryAsync(text, Cancellation);
        var document = Assert.Single(await data.Embedder.EmbedDocumentsAsync([text], Cancellation));

        Assert.InRange(Vectors.Similarity(query, document), -1, 0.99);
    }

    [Theory]
    [InlineData("toys that come alive", "Toy Story", "Jaws")]
    [InlineData("a shark terrorises a beach town", "Jaws", "Toy Story")]
    public async Task SearchWorksOnMeaning(string query, string closer, string further)
    {
        var queryVector = await data.Embedder.EmbedQueryAsync(query, Cancellation);
        var movies = await data.Embedder.EmbedDocumentsAsync(
            [MovieText.ForEmbedding(Movie(closer)), MovieText.ForEmbedding(Movie(further))], Cancellation);

        var (closerSimilarity, furtherSimilarity) =
            (Vectors.Similarity(queryVector, movies[0]), Vectors.Similarity(queryVector, movies[1]));
        Assert.True(closerSimilarity > furtherSimilarity, $"{closer}: {closerSimilarity:F3}, {further}: {furtherSimilarity:F3}");
    }

    [Fact]
    public async Task CancellationStopsEmbedding()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => data.Embedder.EmbedDocumentsAsync(["Toy Story", "Jaws"], cancelled.Token));
    }

    private Movie Movie(string title) => data.Movies.Single(movie => movie.Title == title);

    private sealed record ReferenceEmbedding(string Text, float[] Vector);
}
