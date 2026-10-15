
namespace FileForge.Application.Interfaces;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
