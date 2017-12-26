using System.Collections.Generic;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public interface IPersistentJobQueueMonitoringApi
    {
        IEnumerable<string> GetQueues();
        IEnumerable<long> GetEnqueuedJobIds(string queue, int from, int perPage);
        IEnumerable<long> GetFetchedJobIds(string queue, int from, int perPage);
        EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue);
    }
}