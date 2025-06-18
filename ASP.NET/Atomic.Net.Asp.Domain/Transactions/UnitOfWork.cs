namespace Atomic.Net.Asp.Domain.Transactions;

public sealed class UnitOfWork : IDomainTransaction 
{
    private readonly Stack<IDomainTransaction> _txns = [];

    public void AppendCallback(
        Func<Task>? commit = null,
        Func<Task>? rollback = null
    )
    {
        Append(new DelegateTransaction(commit, rollback));
    }

    public void Append(IDomainTransaction txn) => _txns.Push(txn);

    public async Task CommitAsync()
    {
        foreach(var txn in _txns)
        {
            await txn.CommitAsync();
        }
    }

    public async Task RollbackAsync()
    {
        foreach(var txn in _txns)
        {
            await txn.RollbackAsync();
        }
    }

    public void Dispose()
    {
        foreach(var txn in _txns)
        {
            txn.Dispose();
        }
    }

    public static async Task<UnitOfWork> BeginWithDbTxnAsync(ScopedDbContext db)
    {
        var uow = new UnitOfWork();
        var dbTxn = await db.Database.BeginTransactionAsync();
        var efTxn = new EfCoreTransactionWrapper(dbTxn);
        uow.Append(efTxn);

        return uow;
    }
}
