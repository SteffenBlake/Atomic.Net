using Atomic.Net.Asp.Domain.Transactions;
using Microsoft.Extensions.Logging;

namespace Atomic.Net.Asp.Domain.ThirdParty;

public class ThirdPartyHttpClient(
    ILogger<ThirdPartyHttpClient> logger
)
{
    public async Task<IDomainResult<Unit>> ThirdPartyNotifyFooDeletedAsync(
        UnitOfWork txn, int fooId
    )
    {
        // Call a third party API and delete foo or whatever on it here
        logger.LogInformation($"Soft Deleted fooId: {fooId}!");

        txn.AppendCallback(rollback: () => ThirdPartyNotifyFooUndeletedAsync(fooId));

        return Unit.Default;
    }

    private Task ThirdPartyNotifyFooUndeletedAsync(
        int fooId
    )
    {
        // Call a third party API and delete foo or whatever on it here
        logger.LogInformation($"Nevermind undeleted fooId: {fooId}!");

        return Task.CompletedTask;
    }
}
