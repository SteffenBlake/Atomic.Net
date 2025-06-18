namespace Atomic.Net.Asp.Domain.Transactions;

public interface IDomainTransaction : IDisposable
{
    Task CommitAsync();

    Task RollbackAsync();
}

