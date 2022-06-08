using System;
using System.Globalization;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateFetchedJob : IFetchedJob
    {
        private static readonly ILog Logger = LogProvider.For<FluentNHibernateFetchedJob>();

        private readonly long _id;
        private readonly FluentNHibernateJobStorage _storage;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public FluentNHibernateFetchedJob(
            FluentNHibernateJobStorage storage,
            FetchedJob fetchedJob)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _id = fetchedJob?.Id ?? throw new ArgumentNullException(nameof(fetchedJob));
            JobId = fetchedJob.JobId.ToString(CultureInfo.InvariantCulture);
            Queue = fetchedJob.Queue;
        }

        public string Queue { get; }

        public void Dispose()
        {
            if (_disposed) return;

            if (!_removedFromQueue && !_requeued) Requeue();

            _disposed = true;
        }

        public void RemoveFromQueue()
        {
            Logger.DebugFormat("RemoveFromQueue JobId={0}", JobId);
            _storage.UseStatelessSession(session =>
            {
                session.Query<_JobQueue>().Where(i => i.Id == _id).Delete();
            });

            _removedFromQueue = true;
        }

        public void Requeue()
        {
            Logger.DebugFormat("Requeue JobId={0}", JobId);
            _storage.UseStatelessSession(session =>
            {
                session.CreateQuery(SqlUtil.UpdateJobQueueFetchedAtStatement)
                    .SetParameter(SqlUtil.ValueParameterName, null)
                    .SetParameter(SqlUtil.IdParameterName, _id)
                    .ExecuteUpdate();
            });

            _requeued = true;
        }

        public string JobId { get; }
    }
}