using FileForge.Application.Interfaces;
using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileForge.Infrastructure.Persistence.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly FileForgeDbContext _dbContext;

    public ChunkRepository(FileForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var chunkList = chunks.ToList();

        if (chunkList.Count == 0)
            return;

        await _dbContext.DocumentChunks.AddRangeAsync(
            chunkList, ct);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        IEnumerable<Guid> documentIds,
        int topK,
        CancellationToken ct)
    {
        if (topK <= 0)
            throw new ArgumentOutOfRangeException(nameof(topK));

        ArgumentNullException.ThrowIfNull(documentIds);

        if (queryEmbedding.IsEmpty)
            throw new ArgumentException(
                "Query embedding cannot be empty.",
                nameof(queryEmbedding));

        var ids = documentIds.Distinct().ToArray();

        if (ids.Length == 0)
            return Array.Empty<ScoredChunk>();

        var vector = new Pgvector.Vector(queryEmbedding.ToArray());

        // PostgreSQL calculates cosine distance and returns
        // the nearest chunks first.
        var chunks = await _dbContext.DocumentChunks
            .FromSqlInterpolated($"""
                SELECT *
                FROM document_chunks
                WHERE document_id = ANY ({ids})
                ORDER BY embedding <=> {vector}
                LIMIT {topK}
                """)
            .AsNoTracking()
            .ToListAsync(ct);

        return chunks
            .Select(chunk => new ScoredChunk(
                chunk,
                CosineSimilarity(
                    queryEmbedding.Span,
                    chunk.Embedding.Span)))
            .ToList();
    }

    public async Task DeleteByDocumentIdAsync(
        Guid id,
        CancellationToken ct)
    {
        var chunks = await _dbContext.DocumentChunks
            .Where(chunk => chunk.DocumentId == id)
            .ToListAsync(ct);

        if (chunks.Count == 0)
            return;

        _dbContext.DocumentChunks.RemoveRange(chunks);
    }

    private static double CosineSimilarity(
        ReadOnlySpan<float> left,
        ReadOnlySpan<float> right)
    {
        if (left.Length != right.Length)
            throw new ArgumentException(
                "Embedding dimensions must match.");

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < left.Length; i++)
        {
            dot += (double)left[i] * right[i];
            leftNorm += (double)left[i] * left[i];
            rightNorm += (double)right[i] * right[i];
        }

        if (leftNorm == 0 || rightNorm == 0)
            return 0;

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
