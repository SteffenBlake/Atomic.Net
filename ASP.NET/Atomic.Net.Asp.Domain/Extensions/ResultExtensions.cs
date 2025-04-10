using System.Net;

namespace Atomic.Net.Asp.Domain.Extensions;

public static class ResultExtensions
{
    public static bool IsSuccess<T>(this IResult<T> result)
        where T : class
    {
        return result.Code < HttpStatusCode.BadRequest;
    }
}
