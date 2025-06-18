
using Microsoft.EntityFrameworkCore.Storage;

namespace Atomic.Net.Asp.Domain.Transactions;

public sealed class EfCoreTransactionWrapper(
    IDbContextTransaction txn
) : IDomainTransaction
{
    public async Task CommitAsync()
    {
        await txn.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await txn.RollbackAsync();
    }

    public void Dispose()
    {
        txn.Dispose();
    }
}
