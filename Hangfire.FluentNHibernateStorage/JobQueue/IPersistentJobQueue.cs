using System.Threading;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(StatelessSessionWrapper session, string queue, string jobId);
    }
}