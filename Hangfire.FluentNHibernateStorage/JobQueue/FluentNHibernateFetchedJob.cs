using System;
using System.Globalization;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;
using NHibernate;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public  class FluentNHibernateFetchedJob : IFetchedJob
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly string deleteJobQueueSql = string.Format("delete from {0} where {1}=:id",
            nameof(_JobQueue),
            nameof(_JobQueue.Id));

        private static readonly string updateJobQueueSql =
            Helper.singlefieldupdate(nameof(_JobQueue), nameof(_JobQueue.FetchedAt), nameof(_JobQueue.Id));

        private readonly ISession _connection;
        private readonly int _id;

        private readonly FluentNHibernateStorage _storage;
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public FluentNHibernateFetchedJob(
            FluentNHibernateStorage storage,
            ISession connection,
            FetchedJob fetchedJob)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (connection == null) throw new ArgumentNullException("connection");
            if (fetchedJob == null) throw new ArgumentNullException("fetchedJob");

            _storage = storage;
            _connection = connection;
            _id = fetchedJob.Id;
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

            _storage.ReleaseConnection(_connection);

            _disposed = true;
        }

        public void RemoveFromQueue()
        {
            Logger.TraceFormat("RemoveFromQueue JobId={0}", JobId);

            //todo: unit test
            _connection.CreateQuery(deleteJobQueueSql).SetParameter("id", _id).ExecuteUpdate();

            _removedFromQueue = true;
        }

        public void Requeue()
        {
            Logger.TraceFormat("Requeue JobId={0}", JobId);

            //todo: unit test
            _connection.CreateQuery(updateJobQueueSql).SetParameter("value", null).SetParameter("id", _id)
                .ExecuteUpdate();
            _requeued = true;
        }

        public string JobId { get; }
    }
}