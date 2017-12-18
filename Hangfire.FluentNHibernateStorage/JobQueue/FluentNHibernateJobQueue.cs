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

        private readonly FluentNHibernateJobStorage _storage;

        public FluentNHibernateJobQueue(FluentNHibernateJobStorage storage, FluentNHibernateStorageOptions options)
        {
            Logger.Info("Job queue initialized");
            _storage = storage ?? throw new ArgumentNullException("storage");
            _options = options ?? throw new ArgumentNullException("options");
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException("queues");
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");
            Logger.Info("Attempting to dequeue");


            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();


                try
                {
                    using (var distributedLock =
                        new FluentNHibernateStatelessDistributedLock(_storage, "JobQueue", TimeSpan.FromSeconds(30)).Acquire())
                    {
                        var token = Guid.NewGuid().ToString();

                        if (queues.Any())
                        {
                            var jobQueueFetchedAt = DateTime.UtcNow;
                            var next = jobQueueFetchedAt.AddSeconds(
                                _options.InvisibilityTimeout.Negate().TotalSeconds);
                            using (var transaction = distributedLock.Session.BeginTransaction())
                            {
                                var jobQueue = distributedLock.Session.Query<_JobQueue>().FirstOrDefault(i =>
                                    (i.FetchedAt == null
                                     || i.FetchedAt < next) && queues.Contains(i.Queue));
                                if (jobQueue != null)
                                {
                                    jobQueue.FetchedAt = jobQueueFetchedAt;
                                    jobQueue.FetchToken = token;
                                    distributedLock.Session.Update(jobQueue);
                                    distributedLock.Session.Flush();

                                    transaction.Commit();
                                    Logger.InfoFormat("Dequeued job id {0} for queue {1}", jobQueue.Job.Id, jobQueue.Queue);
                                    var fetchedJob = new FetchedJob
                                    {
                                        Id = jobQueue.Id,
                                        JobId = jobQueue.Job.Id,
                                        Queue = jobQueue.Queue
                                    };
                                    return new FluentNHibernateFetchedJob(_storage, fetchedJob);
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

                cancellationToken.WaitHandle.WaitOne(_options.QueuePollInterval);
                cancellationToken.ThrowIfCancellationRequested();
            } 
        }

        public void Enqueue(IWrappedSession connection, string queue, string jobId)
        {
            
            
            connection.Insert(new _JobQueue
            {
                Job = connection.Query<_Job>().FirstOrDefault(i => i.Id == int.Parse(jobId)),
                Queue = queue
            });
            connection.Flush();
            Logger.InfoFormat("Enqueued JobId={0} Queue={1}", jobId, queue);
        }
    }
}