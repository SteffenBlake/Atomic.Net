using System.Diagnostics.CodeAnalysis;
using Atomic.Net.Asp.Domain.Results;
using Atomic.Net.Asp.Domain.Validation;

namespace Atomic.Net.Asp.Domain.Extensions;

public static class DomainValidatableExtensions 
{
    public static bool HasErrors<TResult>(
        this IDomainValidatable validatable, 
        [NotNullWhen(true)] out DomainResult<TResult>? result
    )
    {
        var validationResult = validatable.Validate();
        if (validationResult.IsSuccess)
        {
            result = null;
            return false;
        }

        result = validationResult.GetProblemDetails();
        return true;
    }
}
