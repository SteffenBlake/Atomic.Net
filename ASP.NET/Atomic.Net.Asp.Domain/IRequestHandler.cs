namespace Atomic.Net.Asp.Domain;

public interface IRequestHandler<TRequest, TResult> 
{
    Task<IDomainResult<TResult>> HandleAsync(
        CommandContext ctx, 
        TRequest request
    );
}
