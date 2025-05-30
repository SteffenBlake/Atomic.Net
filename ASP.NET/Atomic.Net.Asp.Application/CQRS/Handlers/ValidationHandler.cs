using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Application.CQRS.Handlers;

public static class ValidationHandler 
{
    public static async Task<IDomainResult<TResult>> HandleAsync<TResult, TServices, TRequest>(
        RequestContext<TServices> ctx,
        RequestHandler<TResult, TServices, TRequest> handler,
        TRequest request
    )
        where TServices : class
        where TRequest : class
    {
        if (request is IDomainValidatable validatable)
        {
            var validationResult = validatable.Validate();
            if (!validationResult.IsSuccess)
            {
                var problemDetails = validationResult.GetProblemDetails();
                var validationDetails = ValidationDetails<TResult>
                    .FromProblemDetails(problemDetails);
                return validationDetails;
            }
        }

        return await handler(ctx, request);
    }
}
