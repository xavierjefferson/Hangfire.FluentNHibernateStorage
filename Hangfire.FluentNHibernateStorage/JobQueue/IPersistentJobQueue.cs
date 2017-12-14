using System.Threading;
using Hangfire.Storage;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(ISession connection, string queue, string jobId);
    }
}