using System.Net;

namespace Atomic.Net.Asp.Domain;

public class Result
{
    public static IResult<T> Success<T>(T value)
        where T : class
    {
        return new OkResult<T>(value);
    }

    public static IResult<T> Fail<T>(
        HttpStatusCode errorCode, string message, string id
    )
        where T : class
    {
        return new ErrorResult<T>(errorCode, message, id);
    }

    public static IResult<T> NotFound<T>(string target, string id)
        where T : class
    {
        return Fail<T>(
            HttpStatusCode.NotFound,
            $"No {target} found for {id}",
            id
        );
    }

    public static IResult<T> BadRequest<T>(string id)
        where T : class
    {
        return Fail<T>(
            HttpStatusCode.BadRequest,
            "Bad request",
            id
        );
    }

    public static IResult<T> Conflict<T>(string target, string id)
        where T : class
    {
        return Fail<T>(
            HttpStatusCode.Conflict,
            $"Conflict on {target} found for {id}",
            id
        );
    }

    public static IResult<T> Unauthorized<T>()
        where T : class
    {
        return Fail<T>(
            HttpStatusCode.Unauthorized,
            "", ""
        );
    }
}

public class OkResult<T>(T value) : IResult<T>
    where T : class
{
    public T? Value => value;

    public HttpStatusCode Code => HttpStatusCode.OK;

    public string? ErrorMessage => null;

    public string? ErrorId => null;
}

public class ErrorResult<T>(
    HttpStatusCode code, string message, string id
) : IResult<T>
    where T : class
{
    public T? Value => null;

    public HttpStatusCode Code => code;

    public string? ErrorMessage => message;

    public string? ErrorId => id;

}

public interface IResult<T>
    where T : class
{
    public T? Value { get; }

    public HttpStatusCode Code { get; }

    public string? ErrorMessage { get; }

    public string? ErrorId { get; }
}
