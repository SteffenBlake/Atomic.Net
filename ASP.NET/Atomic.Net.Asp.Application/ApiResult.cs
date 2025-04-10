using Atomic.Net.Asp.Domain;
using Atomic.Net.Asp.Domain.Extensions;

namespace Atomic.Net.Asp.Application;

public static class ApiResult
{
    public static ApiResult<T> FromDomainResult<T>(IResult<T> result)
        where T : class
    {
        var output = new ApiResult<T>
        {
            Value = result.Value
        };
        if (!result.IsSuccess())
        {
            output.Error = new()
            {
                Message = result.ErrorMessage!,
                Id = result.ErrorId!
            };
        }
        return output;
    }
}

public class ApiResult<T>
    where T : class
{
    public T? Value { get; set; }

    public ApiError? Error { get; set; }
}

public class ApiError
{
    public required string Message { get; set; }

    public required string Id { get; set; }
}
