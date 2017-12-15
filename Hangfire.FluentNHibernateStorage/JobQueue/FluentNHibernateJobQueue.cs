using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal class FluentNHibernateJobQueue : IPersistentJobQueue
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(FluentNHibernateJobQueue));

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


            do
            {
                cancellationToken.ThrowIfCancellationRequested();


                try
                {
                    using (var distributedLock =
                        new FluentNHibernateDistributedLock(_storage, "JobQueue", TimeSpan.FromSeconds(30)))
                    {
                        var token = Guid.NewGuid().ToString();

                        if (queues.Any())
                        {
                            var next = DateTime.UtcNow.AddSeconds(
                                _options.InvisibilityTimeout.Negate().TotalSeconds);
                            using (var transaction = distributedLock.Session.BeginTransaction())
                            {
                                var jobQueue = distributedLock.Session.Query<_JobQueue>().FirstOrDefault(i =>
                                    (i.FetchedAt == null
                                     || i.FetchedAt < next) && queues.Contains(i.Queue));
                                if (jobQueue != null)
                                {
                                    jobQueue.FetchedAt = DateTime.UtcNow;
                                    jobQueue.FetchToken = token;
                                    distributedLock.Session.Update(jobQueue);
                                    distributedLock.Session.Flush();

                                    transaction.Commit();
                                    fetchedJob = new FetchedJob
                                    {
                                        Id = jobQueue.Id,
                                        JobId = jobQueue.Job.Id,
                                        Queue = jobQueue.Queue
                                    };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex.Message, ex);

                    throw;
                }

                if (fetchedJob == null)
                {
                    cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (fetchedJob == null);

            return new FluentNHibernateFetchedJob(_storage, fetchedJob);
        }

        public void Enqueue(IWrappedSession connection, string queue, string jobId)
        {
            Logger.TraceFormat("Enqueue JobId={0} Queue={1}", jobId, queue);
            connection.Insert(new _JobQueue
            {
                Job = connection.Query<_Job>().FirstOrDefault(i => i.Id == int.Parse(jobId)),
                Queue = queue
            });
            connection.Flush();
        }
    }
}