using Atomic.Net.Asp.Domain.Transactions;

namespace Atomic.Net.Asp.Domain;

public delegate Task<IDomainResult<TResult>> QueryHandler<TDomainContext, TRequest, TResult>(
    RequestContext<TDomainContext> ctx,
    TRequest request
)
    where TDomainContext : class
    where TRequest : class;

public delegate Task<IDomainResult<TResult>> CommandHandler<TDomainContext, TRequest, TResult>(
    UnitOfWork txn,
    RequestContext<TDomainContext> ctx,
    TRequest request
)
    where TDomainContext : class
    where TRequest : class;
