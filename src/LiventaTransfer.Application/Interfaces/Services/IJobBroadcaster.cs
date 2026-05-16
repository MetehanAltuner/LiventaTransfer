namespace LiventaTransfer.Application.Interfaces.Services;

public interface IJobBroadcaster
{
    Task BroadcastJobsChangedAsync(CancellationToken ct = default);
}
