using FileForge.Application.Abstractions;

namespace FileForge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FileForgeDbContext _dbContext;

    public UnitOfWork(FileForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        return new UnitOfWorkTransaction(transaction);
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }
}
