using System.Threading;
using Hangfire.Storage;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
      interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(IWrappedSession connection, string queue, string jobId);
    }
}