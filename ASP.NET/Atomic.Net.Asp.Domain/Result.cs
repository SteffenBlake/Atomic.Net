using Validly.Details;

namespace Atomic.Net.Asp.Domain;

public interface IDomainResult<out TResult> {}

public readonly record struct ValidationDetails<TResult>(
    string Type,
    string Title,
    int? Status,
    IReadOnlyList<ValidationErrorDetail> Errors
): IDomainResult<TResult>
{
    public static ValidationDetails<TResult> FromProblemDetails(ValidationResultDetails result)
    {
        return new (result.Type, result.Title, result.Status, result.Errors);
    }
}

public readonly struct NotFound<TResult>(
    string name, object idValue
) : IDomainResult<TResult>
{
    public string Message { get; } = $"No {name} found with id '{idValue}'";
}

public readonly struct Conflict<TResult>(
    string name, object value
) : IDomainResult<TResult>
{
    public string Message { get; } = $"{name} already exists with value of '{value}'";
}

public readonly record struct Unauthorized<TResult>() : IDomainResult<TResult>;


public readonly record struct Unit : IDomainResult<Unit>
{
    public static readonly Unit Default = new();
}
