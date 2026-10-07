namespace OptixMovies.Data.Sqlite;

/// <summary>The one row of the EmbeddingStatus table: whether movies need embedding, and what embedded them.</summary>
internal sealed class EmbeddingStatusEntity
{
    public int Id { get; set; }

    /// <summary>Set by anything that adds or changes movies; cleared by the embedder.</summary>
    public bool Required { get; set; }

    public string? Model { get; set; }

    public string? Variant { get; set; }
}
