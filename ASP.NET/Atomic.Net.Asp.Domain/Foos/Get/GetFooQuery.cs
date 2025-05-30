using Validly.Extensions.Validators.Common;
using Validly.Extensions.Validators.Numbers;
using Atomic.Net.Asp.Domain.Validation;
using Validly;

namespace Atomic.Net.Asp.Domain.Foos.Get;

[Validatable]
public partial class GetFooQuery : IDomainValidatable
{
    [Required]
    [Between(1, 9999)]
    public required int FooId { get; set; }

}
