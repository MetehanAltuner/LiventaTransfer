using LiventaTransfer.API.Hubs;
using LiventaTransfer.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace LiventaTransfer.API.Realtime;

public sealed class JobBroadcaster : IJobBroadcaster
{
    public const string EventMethod = "JobListEvent";
    public const string JobsChangedEvent = "JOBS_CHANGED";

    private readonly IHubContext<JobsHub> _hub;

    public JobBroadcaster(IHubContext<JobsHub> hub) => _hub = hub;

    public Task BroadcastJobsChangedAsync(CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(
            EventMethod,
            new { job_list_event = JobsChangedEvent },
            ct);
}
