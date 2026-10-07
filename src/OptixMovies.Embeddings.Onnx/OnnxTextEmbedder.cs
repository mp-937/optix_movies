using System.Text.RegularExpressions;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Tokenizers;

using OptixMovies.Core;

namespace OptixMovies.Embeddings.Onnx;

/// <summary>
/// Embeds text in-process with bge-small-en-v1.5, quantised to 8 bits, from the Model folder next to the app.
/// One instance can serve many threads at once.
/// </summary>
public sealed partial class OnnxTextEmbedder : ITextEmbedder, IDisposable
{
    private const int MaxTokens = 512;
    private const string Punctuation = @"\p{P}\x21-\x2F\x3A-\x40\x5B-\x60\x7B-\x7E"; // ASCII symbols count too

    // BAAI's recommended instruction before a short search for longer passages. Documents are embedded as they are.
    private const string QueryInstruction = "Represent this sentence for searching relevant passages: ";

    private readonly BertTokenizer tokenizer;
    private readonly InferenceSession session;

    /// <exception cref="FileNotFoundException">The model's files are missing.</exception>
    public OnnxTextEmbedder()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Model");
        var vocabulary = Path.Combine(folder, "vocab.txt");
        var model = Path.Combine(folder, "model.onnx");

        foreach (var file in (string[])[vocabulary, model])
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"No embedding model file at {file}.", file);
            }
        }

        // Matches the model's original tokenizer: lowercase, without accents, split into words as Words() describes.
        tokenizer = BertTokenizer.Create(vocabulary, new BertOptions
        {
            RemoveNonSpacingMarks = true,
            PreTokenizer = new RegexPreTokenizer(Words(), null),
        });
        session = new InferenceSession(model);
    }

    public string Model => "bge-small-en-v1.5";

    public string Variant => "int8";

    public int Dimensions => 384;

    public Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> documents, CancellationToken cancellationToken = default)
    {
        var vectors = new float[documents.Count][];

        for (var i = 0; i < documents.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            vectors[i] = Embed(documents[i]);
        }

        return Task.FromResult<IReadOnlyList<float[]>>(vectors);
    }

    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default) =>
        Task.FromResult(Embed(QueryInstruction + query));

    public void Dispose() => session.Dispose();

    // One text per run. The 8-bit model scales its arithmetic to everything in a run, so texts embedded together
    // would shift one another's vectors, and a movie's vector would depend on the movies beside it.
    private float[] Embed(string text)
    {
        long[] inputIds = [.. tokenizer.EncodeToIds(text, MaxTokens, addSpecialTokens: true, out _, out _)];
        var attentionMask = new long[inputIds.Length];
        Array.Fill(attentionMask, 1);

        long[] shape = [1, inputIds.Length];
        using var inputIdsValue = OrtValue.CreateTensorValueFromMemory(inputIds, shape);
        using var attentionMaskValue = OrtValue.CreateTensorValueFromMemory(attentionMask, shape);
        using var tokenTypeIdsValue = OrtValue.CreateTensorValueFromMemory(new long[inputIds.Length], shape);
        using var runOptions = new RunOptions();
        using var outputs = session.Run(
            runOptions,
            ["input_ids", "attention_mask", "token_type_ids"],
            [inputIdsValue, attentionMaskValue, tokenTypeIdsValue],
            ["last_hidden_state"]);

        // The model's output for the first token, [CLS], stands for the whole text.
        return Normalize(outputs[0].GetTensorDataAsSpan<float>()[..Dimensions]);
    }

    // Words as the original tokenizer splits them: each punctuation mark or ASCII symbol is a word of its own, and
    // anything else between spaces or invisible characters is one word. Microsoft's default would drop symbols, such
    // as the "$" in "$5 million".
    [GeneratedRegex("[" + Punctuation + @"]|[^\s\p{Cc}\p{Cf}" + Punctuation + "]+")]
    private static partial Regex Words();

    // Scales a vector to length 1, so that dot products are cosine similarities.
    private static float[] Normalize(ReadOnlySpan<float> values)
    {
        var vector = values.ToArray();
        var length = MathF.Sqrt(vector.Sum(value => value * value));

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= length;
        }

        return vector;
    }
}
