using System.Net;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Application;

public class RequestRunner<TRequest, TResult>(
    AppDbContext db, 
    IRequestHandler<TRequest, TResult> handler
)
    where TRequest : class
{
    public async Task<IDomainResult<TResult>> HandleCommandAsync(HttpContext httpContext, TRequest request)
    {
        using var txn = await db.Database.BeginTransactionAsync();
        try
        {
            var result = await HandleQueryAsync(httpContext, request);
            if (result is TResult)
            {
                await txn.CommitAsync();
            }
            else
            {
                await txn.RollbackAsync();
            }
            return result;
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }
    }

    public async Task<IDomainResult<TResult>> HandleQueryAsync(HttpContext httpContext, TRequest request)
    {
        if (request is IDomainValidatable validatable)
        {
            var validationResult = validatable.Validate();
            if (!validationResult.IsSuccess)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var problemDetails = validationResult.GetProblemDetails();
                var validationDetails = ValidationDetails<TResult>
                    .FromProblemDetails(problemDetails);
                return validationDetails;
            }
        }

        var userId = httpContext.User.Identity?.Name;

        var result = await handler.HandleAsync(new(db, userId), request);

        httpContext.Response.StatusCode = result switch
        {
            TResult => (int)HttpStatusCode.OK,
            NotFound<TResult> => (int)HttpStatusCode.NotFound,
            Conflict<TResult> => (int)HttpStatusCode.Conflict,
            Unauthorized<TResult> => (int)HttpStatusCode.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };

        return result;
    }
}
