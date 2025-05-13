using System.Net;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Application.Controllers;

public static class Handlers
{
    public static async Task<IDomainResult<TResult>> HandleCommandAsync<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<IDomainResult<TResult>>> query
    )
        where TRequest : class
    {
        using var txn = await webContext.DB.Database.BeginTransactionAsync();
        try
        {
            var result = await HandleQueryAsync(httpContext, webContext, request, query);
            if (httpContext.Response.StatusCode >= 300)
            {
                await txn.RollbackAsync();
            }
            else
            {
                await txn.CommitAsync();
            }
            return result;
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }
    }

    public static async Task<IDomainResult<TResult>> HandleQueryAsync<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<IDomainResult<TResult>>> action
    )
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

        var result = await action(webContext.Compile(httpContext), request);

        httpContext.Response.StatusCode = result switch
        {
            TResult => (int)HttpStatusCode.OK,
            NotFound<TResult> => (int)HttpStatusCode.NotFound,
            Conflict<TResult> => (int)HttpStatusCode.Conflict,
            Unauthorized<TResult> => (int)HttpStatusCode.Unauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        return result;
    }
}
