using FileForge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Application.Interfaces;

public record ScoredChunk(DocumentChunk Chunk, double Score);

public interface IChunkRepository
{
    Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default);
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> queryEmbedding, IEnumerable<Guid> documentIds, int topK, CancellationToken ct = default);
    Task<IReadOnlyList<ScoredChunk>> SearchAllAsync(ReadOnlyMemory<float> queryEmbedding, int topK, CancellationToken ct = default);
    Task DeleteByDocumentIdAsync(Guid id, CancellationToken ct = default);
}
