using System;
using System.Globalization;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateFetchedJob : IFetchedJob
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string DeleteJobQueueSql = string.Format("delete from {0} where {1}=:{2}",
            nameof(_JobQueue),
            nameof(_JobQueue.Id), Helper.IdParameterName);

        private static readonly string UpdateJobQueueSql =
            Helper.GetSingleFieldUpdateSql(nameof(_JobQueue), nameof(_JobQueue.FetchedAt), nameof(_JobQueue.Id));

        private readonly int _id;


        private readonly FluentNHibernateStorage _storage;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public FluentNHibernateFetchedJob(
            FluentNHibernateStorage storage,
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
            using (var session = _storage.GetStatefulSession())
            {
               
                session.CreateQuery(DeleteJobQueueSql).SetParameter(Helper.IdParameterName, _id).ExecuteUpdate();
            }
            _removedFromQueue = true;
        }

        public void Requeue()
        {
            Logger.TraceFormat("Requeue JobId={0}", JobId);
            using (var session = _storage.GetStatefulSession())
            {
           
                session.CreateQuery(UpdateJobQueueSql).SetParameter(Helper.ValueParameterName, null)
                    .SetParameter(Helper.IdParameterName, _id)
                    .ExecuteUpdate();
            }
            _requeued = true;
        }

        public string JobId { get; }
    }
}