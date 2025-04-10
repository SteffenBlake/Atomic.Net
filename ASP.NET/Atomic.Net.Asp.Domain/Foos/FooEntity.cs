using System.ComponentModel.DataAnnotations;

namespace Atomic.Net.Asp.Domain.Foos;

public class FooEntity
{
    [Key]
    public int Id { get; set; }

    public required string SensitiveData { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }
}
