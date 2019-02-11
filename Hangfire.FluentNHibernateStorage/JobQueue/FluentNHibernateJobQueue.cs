using System;
using System.Data;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateJobQueue : IPersistentJobQueue
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
            Logger.Debug("Attempting to dequeue");

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();


                    try
                    {
                        using (new FluentNHibernateDistributedLock(_storage, "JobQueue", TimeSpan.FromSeconds(60))
                            .Acquire())
                        {
                            var fluentNHibernateFetchedJob = SqlUtil.WrapForTransaction(() =>
                            {
                                var token = Guid.NewGuid().ToString();

                                if (queues.Any())
                                {
                                    using (var session = _storage.GetSession())
                                    {
                                        using (var transaction =
                                            session.BeginTransaction(IsolationLevel.Serializable))
                                        {
                                            var jobQueueFetchedAt = _storage.UtcNow;
                                            var next = jobQueueFetchedAt.AddSeconds(
                                                _options.InvisibilityTimeout.Negate().TotalSeconds);
                                            var jobQueue = session.Query<_JobQueue>()
                                                .FirstOrDefault(i =>
                                                    (i.FetchedAt == null
                                                     || i.FetchedAt < next) && queues.Contains(i.Queue));
                                            if (jobQueue != null)
                                            {
                                                jobQueue.FetchedAt = jobQueueFetchedAt;
                                                jobQueue.FetchToken = token;
                                                session.Update(jobQueue);
                                                session.Flush();

                                                transaction.Commit();
                                                Logger.DebugFormat("Dequeued job id {0} from queue {1}",
                                                    jobQueue.Job.Id,
                                                    jobQueue.Queue);
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

                                return null;
                            });
                            if (fluentNHibernateFetchedJob != null)
                            {
                                return fluentNHibernateFetchedJob;
                            }
                        }
                    }
                    catch (FluentNHibernateDistributedLockException)
                    {
                        // do nothing
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
            catch (OperationCanceledException)
            {
                Logger.Debug("The dequeue operation was cancelled.");
                return null;
            }
        }

        public void Enqueue(SessionWrapper session, string queue, string jobId)
        {
            var converter = StringToInt32Converter.Convert(jobId);
            if (!converter.Valid)
            {
                return;
            }

            session.Insert(new _JobQueue
            {
                Job = session.Query<_Job>().SingleOrDefault(i => i.Id == converter.Value),
                Queue = queue
            });
            session.Flush();
            Logger.InfoFormat("Enqueued JobId={0} Queue={1}", jobId, queue);
        }
    }
}