
namespace FileForge.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct);
}
