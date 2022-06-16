using System;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateJobQueue : IPersistentJobQueue
    {
        private static readonly ILog Logger = LogProvider.For<FluentNHibernateJobQueue>();

        private readonly FluentNHibernateJobStorage _storage;

        public FluentNHibernateJobQueue(FluentNHibernateJobStorage storage)
        {
            Logger.Debug("Job queue initialized");
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));
            if (queues.Length == 0) throw new ArgumentException("Queue array must be non-empty.", "queues");
            Logger.Debug("Attempting to dequeue");

            FluentNHibernateFetchedJob fetchedJob = null;
            var timeoutSeconds = _storage.Options.InvisibilityTimeout.Negate().TotalSeconds;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var fluentNHibernateDistributedLock = FluentNHibernateDistributedLock.Acquire(_storage, "JobQueue",
                        _storage.Options.JobQueueDistributedLockTimeout);
                    using (fluentNHibernateDistributedLock)
                    {
                        fetchedJob = SqlUtil.WrapForTransaction(() =>
                        {
                            return _storage.UseStatelessSessionInTransaction(session =>
                            {
                                var jobQueueFetchedAt = _storage.UtcNow;

                                var cutoff = jobQueueFetchedAt.AddSeconds(timeoutSeconds);
                                if (Logger.IsDebugEnabled())
                                    Logger.Debug(string.Format("Getting jobs where {0}=null or {0}<{1}",
                                        nameof(_JobQueue.FetchedAt), cutoff));

                                var jobQueue = session.Query<_JobQueue>()
                                    .FirstOrDefault(i =>
                                        (i.FetchedAt == null
                                         || i.FetchedAt < cutoff) && queues.Contains(i.Queue));
                                if (jobQueue != null)
                                {
                                    jobQueue.FetchToken = Guid.NewGuid().ToString();
                                    jobQueue.FetchedAt = jobQueueFetchedAt;

                                    session.Update(jobQueue);

                                    Logger.DebugFormat("Dequeued job id {0} from queue {1}",
                                        jobQueue.Job.Id,
                                        jobQueue.Queue);
                                    var tmp = new FetchedJob
                                    {
                                        Id = jobQueue.Id,
                                        JobId = jobQueue.Job.Id,
                                        Queue = jobQueue.Queue
                                    };
                                    return new FluentNHibernateFetchedJob(_storage, tmp);
                                }


                                return null;
                            });
                        });
                    }

                    if (fetchedJob != null) return fetchedJob;
                }
                catch (DistributedLockTimeoutException)
                {
                    Logger.Debug("Distributed lock acquisition timeout was exceeded");
                }

                cancellationToken.PollForCancellation(_storage.Options.QueuePollInterval);
            } while (fetchedJob == null);

            return fetchedJob;
        }

        public void Enqueue(StatelessSessionWrapper session, string queue, string jobId)
        {
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid)
                return;

            session.Insert(new _JobQueue
            {
                Job = session.Query<_Job>().SingleOrDefault(i => i.Id == converter.Value),
                Queue = queue
            });

            Logger.DebugFormat("Enqueued JobId={0} Queue={1}", jobId, queue);
        }
    }
}