using Validly;

namespace Atomic.Net.Asp.Domain.Validation;

public interface IDomainValidatable
{
    ValidationResult Validate();
}
