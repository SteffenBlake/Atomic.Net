using dotVariant;
using Validly.Details;

namespace Atomic.Net.Asp.Domain.Results;

[Variant]
public partial class DomainResult<T>
{
    static partial void VariantOf(
        T t, 
        NotFound notFound, 
        Conflict conflict,
        Unauthorized unauthorized,
        ValidationResultDetails validationResultDetails
    );
}

public readonly struct NotFound(
    string name, object idValue
)
{
    public string Message { get; } = $"No {name} found with id '{idValue}'";
}

public readonly struct Conflict(
    string name, object value
)
{
    public string Message { get; } = $"{name} already exists with value of '{value}'";
}

public readonly record struct Unauthorized();
