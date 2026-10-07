namespace OptixMovies.Core;

/// <summary>
/// A search for the movies nearest <paramref name="Vector"/>, which <paramref name="Model"/> and
/// <paramref name="Variant"/> made: only vectors from the same model and variant can be compared with it.
/// </summary>
public sealed record VectorQuery(float[] Vector, string Model, string Variant, int Limit);
