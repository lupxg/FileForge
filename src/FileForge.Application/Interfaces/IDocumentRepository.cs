using FileForge.Domain.Entities;

namespace FileForge.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid documentId, CancellationToken ct);
}
