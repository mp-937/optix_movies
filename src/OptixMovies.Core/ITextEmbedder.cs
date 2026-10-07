namespace OptixMovies.Core;

/// <summary>
/// Turns text into embeddings: vectors that lie close together when texts mean similar things. Each vector has length
/// 1, so the dot product of two is their cosine similarity. Vectors from different models, or different variants of
/// one model, can't be compared.
/// </summary>
public interface ITextEmbedder
{
    /// <summary>The model's name, such as "bge-small-en-v1.5".</summary>
    string Model { get; }

    /// <summary>Which build of the model, such as "int8".</summary>
    string Variant { get; }

    /// <summary>The number of values in every vector.</summary>
    int Dimensions { get; }

    /// <summary>Embeds texts to be searched, such as movie descriptions.</summary>
    Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> documents, CancellationToken cancellationToken = default);

    /// <summary>Embeds a search. Some models embed searches differently from the texts they search.</summary>
    Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default);
}
