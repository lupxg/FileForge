using FileForge.Domain.Entities;

namespace FileForge.Infrastructure.Persistence.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid documentId, CancellationToken ct);
}
