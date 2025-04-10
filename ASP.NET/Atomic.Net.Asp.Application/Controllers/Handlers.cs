using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Extensions;

namespace Atomic.Net.Asp.Application.Controllers;

public static class Handlers
{
    public static async Task<ApiResult<TResult>> HandleQuery<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<IResult<TResult>>> query
    )
        where TRequest : class
        where TResult : class
    {
        var result = await query(webContext.Compile(httpContext), request);
        httpContext.Response.StatusCode = (int)result.Code;
        return ApiResult.FromDomainResult(result);
    }

    public static async Task<ApiResult<TResult>> HandleCommand<TRequest, TResult>(
        HttpContext httpContext,
        WebContext<TRequest> webContext,
        TRequest request,
        Func<CommandContext<TRequest>, TRequest, Task<IResult<TResult>>> query
    )
        where TRequest : class
        where TResult : class
    {
        using var txn = await webContext.DB.Database.BeginTransactionAsync();
        try
        {

            var result = await query(webContext.Compile(httpContext), request);
            if (result.IsSuccess())
            {
                await txn.CommitAsync();
            }
            else
            {
                await txn.RollbackAsync();
            }

            httpContext.Response.StatusCode = (int)result.Code;
            return ApiResult.FromDomainResult(result);
        }
        catch (Exception)
        {
            txn.Rollback();
            throw;
        }

    }
}
