using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal class FluentNHibernateJobQueue : IPersistentJobQueue
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(FluentNHibernateJobQueue));

        private static readonly string DequeueSql = string.Format("update {0} set {1} = :current, {2} = :token " +
                                                                  "where ({1} is null or {1} < :next ) " +
                                                                  "   and {3} in (:queues)", nameof(_JobQueue),
            nameof(_JobQueue.FetchedAt), nameof(_JobQueue.FetchToken), nameof(_JobQueue.Queue));

        private readonly FluentNHibernateStorageOptions _options;

        private readonly FluentNHibernateStorage _storage;

        public FluentNHibernateJobQueue(FluentNHibernateStorage storage, FluentNHibernateStorageOptions options)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _options = options ?? throw new ArgumentNullException("options");
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            FetchedJob fetchedJob = null;
            ISession connection;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection = _storage.GetStatefulSession();

                try
                {
                    using (new FluentNHibernateDistributedLock(_storage, "JobQueue", TimeSpan.FromSeconds(30)))
                    {
                        var token = Guid.NewGuid().ToString();
                        int updateCount=0;
                        if (queues.Any())
                        {
                            var query = connection.CreateQuery(DequeueSql).SetParameter("current", DateTime.UtcNow)
                                .SetParameter("next",
                                    DateTime.UtcNow.AddSeconds(_options.InvisibilityTimeout.Negate().TotalSeconds))
                                .SetParameter("token", token).SetParameterList("queues", queues);

                            updateCount = query.ExecuteUpdate();
                        }

                        if (updateCount != 0)
                        {
                            fetchedJob =
                                connection
                                    .Query<_JobQueue>().Where(i => i.FetchToken == token).Select(i =>
                                        new FetchedJob {Id = i.Id, JobId = i.Job.Id, Queue = i.Queue}
                                    ).SingleOrDefault();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex.Message, ex);
                    _storage.ReleaseSession(connection);
                    throw;
                }

                if (fetchedJob == null)
                {
                    _storage.ReleaseSession(connection);

                    cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (fetchedJob == null);

            return new FluentNHibernateFetchedJob(_storage, connection, fetchedJob);
        }

        public void Enqueue(ISession connection, string queue, string jobId)
        {
            Logger.TraceFormat("Enqueue JobId={0} Queue={1}", jobId, queue);
            connection.Save(new _JobQueue {Job = new _Job {Id = int.Parse(jobId)}, Queue = queue});
            connection.Flush();
        }
    }
}