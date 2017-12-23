using System;
using System.Globalization;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateFetchedJob : IFetchedJob
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly int _id;


        private readonly FluentNHibernateJobStorage _storage;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public FluentNHibernateFetchedJob(
            FluentNHibernateJobStorage storage,
            FetchedJob fetchedJob)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");

            _id = fetchedJob?.Id ?? throw new ArgumentNullException("fetchedJob");
            JobId = fetchedJob.JobId.ToString(CultureInfo.InvariantCulture);
            Queue = fetchedJob.Queue;
        }

        public string Queue { get; }

        public void Dispose()
        {
            if (_disposed) return;

            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }


            _disposed = true;
        }

        public void RemoveFromQueue()
        {
            Logger.TraceFormat("RemoveFromQueue JobId={0}", JobId);
            using (var session = _storage.GetSession())
            {
                session.CreateQuery(SQLHelper.DeleteJobQueueStatement)
                    .SetParameter(SQLHelper.IdParameterName, _id)
                    .ExecuteUpdate();
            }
            _removedFromQueue = true;
        }

        public void Requeue()
        {
            Logger.TraceFormat("Requeue JobId={0}", JobId);
            using (var session = _storage.GetSession())
            {
                session.CreateQuery(SQLHelper.UpdateJobQueueFetchedAtStatement)
                    .SetParameter(SQLHelper.ValueParameterName, null)
                    .SetParameter(SQLHelper.IdParameterName, _id)
                    .ExecuteUpdate();
            }
            _requeued = true;
        }

        public string JobId { get; }
    }
}