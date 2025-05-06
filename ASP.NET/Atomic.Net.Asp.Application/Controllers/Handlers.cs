using System.Net;
using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Validation;
using System.Text.Json;

namespace Atomic.Net.Asp.Application.Controllers;

public static class Handlers
{
    public static async Task<string> HandleQuery<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<Result<TResult>>> query
    )
        where TRequest : class
        where TResult : class
    {
        var (data, _) = await HandleAsync(httpContext, webContext, request, query);
        return data;
    }

    public static async Task<string> HandleCommand<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<Result<TResult>>> query
    )
        where TRequest : class
        where TResult : class
    {
        using var txn = await webContext.DB.Database.BeginTransactionAsync();
        try
        {
            var (data, success) = await HandleAsync(httpContext, webContext, request, query);
            if (success)
            {
                // Success
                await txn.CommitAsync();
            }
            else
            {
                // Fail
                await txn.RollbackAsync();
            }
            return data;
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }
    }

    private static async Task<(string Data, bool Success)> HandleAsync<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<Result<TResult>>> action
    )
        where TRequest : class
        where TResult : class
    {
        if (request is IDomainValidatable validatable)
        {
            var validationResult = validatable.Validate();
            if (!validationResult.IsSuccess)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var problemDetails = validationResult.GetProblemDetailsJson();
                return (problemDetails, false);
            }
        }

        // TODO : There's *gotta* be a better way to handle Json Serializing out
        // a discriminated union type...
        var result = await action(webContext.Compile(httpContext), request);
        if (result.TryMatch(out TResult? success))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            return (JsonSerializer.Serialize(success), true);
        }
        if (result.TryMatch(out NotFound notFound))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return (JsonSerializer.Serialize(notFound), false);
        }
        if (result.TryMatch(out Conflict conflict))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
            return (JsonSerializer.Serialize(conflict), false);
        }
        if (result.TryMatch(out Unauthorized unauthorized))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return (JsonSerializer.Serialize(unauthorized), false);
        }

        throw new ArgumentOutOfRangeException(nameof(action));
    }
}
