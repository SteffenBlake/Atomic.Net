using Atomic.Net.Asp.Domain.Validation;
using Validly;
using Validly.Extensions.Validators.Common;
using Validly.Extensions.Validators.Numbers;

namespace Atomic.Net.Asp.Domain.Foos.Delete;

[Validatable]
public partial class DeleteFooCommand : IDomainValidatable
{
    [Required]
    [Between(1, 9999)]
    public required int FooId { get; set; }
}
