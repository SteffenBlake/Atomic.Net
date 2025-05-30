namespace Atomic.Net.Asp.Domain;

public delegate Task<IDomainResult<TResult>> RequestHandler<TResult, TServices, TRequest>(
    RequestContext<TServices> ctx,
    TRequest request
)
    where TServices : class
    where TRequest : class;
