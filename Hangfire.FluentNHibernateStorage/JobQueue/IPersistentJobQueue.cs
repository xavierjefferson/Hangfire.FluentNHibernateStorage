using System.Threading;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(IWrappedSession session, string queue, string jobId);
    }
}