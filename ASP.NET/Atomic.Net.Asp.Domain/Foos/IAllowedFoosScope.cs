namespace Atomic.Net.Asp.Domain.Foos;

public interface IFooScoped 
{
    DomainScope AllowedFooIds { get; }
}
