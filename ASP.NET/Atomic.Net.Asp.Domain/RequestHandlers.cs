using Atomic.Net.Asp.Domain.Results;
using Atomic.Net.Asp.Domain.Transactions;

namespace Atomic.Net.Asp.Domain;

public delegate Task<DomainResult<TResult>> QueryHandler<TDomainContext, TRequest, TResult>(
    RequestContext<TDomainContext> ctx,
    TRequest request
)
    where TDomainContext : class
    where TRequest : class;

public delegate Task<DomainResult<TResult>> CommandHandler<TDomainContext, TRequest, TResult>(
    UnitOfWork txn,
    RequestContext<TDomainContext> ctx,
    TRequest request
)
    where TDomainContext : class
    where TRequest : class;
