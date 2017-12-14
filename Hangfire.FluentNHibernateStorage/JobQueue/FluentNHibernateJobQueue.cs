using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal class FluentNHibernateJobQueue : IPersistentJobQueue
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(FluentNHibernateJobQueue));
        private readonly FluentNHibernateStorageOptions _options;

        private readonly FluentNHibernateStorage _storage;

        public FluentNHibernateJobQueue(FluentNHibernateStorage storage, FluentNHibernateStorageOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");

            _storage = storage;
            _options = options;
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");

            FetchedJob fetchedJob = null;
            ISession connection = null;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection = _storage.CreateAndOpenSession();

                try
                {
                    using (new FluentNHibernateDistributedLock(_storage, "JobQueue", TimeSpan.FromSeconds(30)))
                    {
                        var token = Guid.NewGuid().ToString();

                        int nUpdated = connection.Execute(
                            "update JobQueue set FetchedAt = UTC_TIMESTAMP(), FetchToken = @fetchToken " +
                            "where (FetchedAt is null or FetchedAt < DATE_ADD(UTC_TIMESTAMP(), INTERVAL @timeout SECOND)) " +
                            "   and Queue in @queues " +
                            "LIMIT 1;",
                            new
                            {
                                queues,
                                timeout = _options.InvisibilityTimeout.Negate().TotalSeconds,
                                fetchToken = token
                            });

                        if (nUpdated != 0)
                        {
                            fetchedJob =
                                connection
                                    .Query<_JobQueue>().Where(i=>i.FetchToken == token).Select(i=>
                                    new FetchedJob() { Id=i.Id,JobId = i.Job.Id,Queue=i.Queue}
                                    ).SingleOrDefault();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Logger.ErrorException(ex.Message, ex);
                    _storage.ReleaseConnection(connection);
                    throw;
                }

                if (fetchedJob == null)
                {
                    _storage.ReleaseConnection(connection);

                    cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (fetchedJob == null);

            return new FluentNHibernateFetchedJob(_storage, connection, fetchedJob);
        }

        public void Enqueue(ISession connection, string queue, string jobId)
        {
            Logger.TraceFormat("Enqueue JobId={0} Queue={1}", jobId, queue);
            connection.Save(new _JobQueue(){Job=new _Job{Id = int.Parse(jobId)}, Queue = queue} );
            connection.Flush();
        }
    }
}