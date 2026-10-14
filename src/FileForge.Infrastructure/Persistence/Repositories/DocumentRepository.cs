using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly FileForgeDbContext _dbContext;

    public DocumentRepository(FileForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Document?> GetAsync(
        Guid documentId,
        CancellationToken ct)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                document => document.Id == documentId,
                ct);
    }
}
