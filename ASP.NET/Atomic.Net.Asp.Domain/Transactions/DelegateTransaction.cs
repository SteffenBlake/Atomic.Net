namespace Atomic.Net.Asp.Domain.Transactions;

public sealed class DelegateTransaction(
    Func<Task>? commit = null,
    Func<Task>? rollback = null
) : IDomainTransaction
{
    public Task CommitAsync() => commit?.Invoke() ?? throw new NotImplementedException();

    public Task RollbackAsync() => rollback?.Invoke() ?? throw new NotImplementedException();

    public void Dispose() {}
}
