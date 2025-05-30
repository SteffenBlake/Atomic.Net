using System.Linq.Expressions;

namespace Atomic.Net.Asp.Domain.Foos.Get;

public class GetFooResponse : IDomainResult<GetFooResponse>
{
    public required int Id { get; set; }

    public required string FullName { get; set; }

    public required string SensitiveData { get; set; }

    public static Expression<Func<FooEntity, GetFooResponse>> FromEntity(
        CommandContext _
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
