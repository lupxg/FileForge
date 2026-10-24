using FileForge.Application.Interfaces;

namespace FileForge.Application.Features.Documents.Search;

public sealed class SearchDocumentsHandler(
    IEmbeddingService embeddingService,
    IChunkRepository chunkRepository)
{
    private const int DefaultTopK = 5;
    private const int MaxTopK = 20;
    private const int ExpectedEmbeddingDimensions = 1536;

    public async Task<IReadOnlyList<SearchResultDto>> HandleAsync(
        string query,
        int topK = DefaultTopK,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(query));
        }

        if (topK is < 1 or > MaxTopK)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                $"TopK must be between 1 and {MaxTopK}.");
        }

        // 1. Convert the user's question into a vector.
        var queryEmbedding = await embeddingService.EmbedAsync(
            query.Trim(),
            ct);

        if (queryEmbedding.Length != ExpectedEmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Expected an embedding with " +
                $"{ExpectedEmbeddingDimensions} dimensions, " +
                $"but received {queryEmbedding.Length}.");
        }

        // 2. Retrieve the most similar chunks across all documents.
        var results = await chunkRepository.SearchAllAsync(
            queryEmbedding,
            topK,
            ct);

        // 3. Map domain entities to API-friendly application DTOs.
        return results
            .Select(result => new SearchResultDto(
                result.Chunk.DocumentId,
                result.Chunk.Index,
                result.Chunk.Content,
                result.Score))
            .ToList();
    }
}
