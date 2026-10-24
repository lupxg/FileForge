using FileForge.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace FileForge.Infrastructure.Embeddings;

public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    public const string ModelName = "text-embedding-3-small";
    public const int Dimensions = 1536;

    private readonly IEmbeddingGenerator<string, Embedding<float>>
        _generator;

    public OpenAIEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    public async Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var embeddings = await EmbedBatchAsync([text], ct);
        return embeddings[0];
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var inputs = texts.ToList();

        if (inputs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Embedding inputs cannot contain empty text.",
                nameof(texts));
        }

        ct.ThrowIfCancellationRequested();

        if (inputs.Count == 0)
        {
            return Array.Empty<ReadOnlyMemory<float>>();
        }

        var generated = await _generator.GenerateAsync(
            inputs,
            cancellationToken: ct);

        if (generated.Count != inputs.Count)
        {
            throw new InvalidOperationException(
                $"Expected {inputs.Count} embeddings, " +
                $"but received {generated.Count}.");
        }

        var vectors = generated
            .Select(embedding =>
                (ReadOnlyMemory<float>)embedding.Vector.ToArray())
            .ToList();

        foreach (var vector in vectors)
        {
            if (vector.Length != Dimensions)
            {
                throw new InvalidOperationException(
                    $"Expected {Dimensions} dimensions, " +
                    $"but received {vector.Length}.");
            }
        }

        return vectors;
    }
}
