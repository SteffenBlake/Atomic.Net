using System.Linq.Expressions;

namespace Atomic.Net.Asp.Domain.Foos;

public class GetFooResponse
{
    public required int Id { get; set; }

    public required string FullName { get; set; }

    public required string SensitiveData { get; set; }

    public static Expression<Func<FooEntity, GetFooResponse>> FromEntity<T>(
        CommandContext<T> _
    )
    {
        return m => new GetFooResponse()
        {
            Id = m.Id,
            FullName = m.FirstName + " " + m.LastName,
            SensitiveData = m.SensitiveData
        };
    }
}
