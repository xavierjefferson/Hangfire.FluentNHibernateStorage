using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private static readonly TimeSpan QueuesCacheTimeout = TimeSpan.FromSeconds(5);
        private readonly object Mutex = new object();

        private readonly FluentNHibernateJobStorage _storage;
        private DateTime _cacheUpdated;
        private List<string> _queuesCache = new List<string>();

        public FluentNHibernateJobQueueMonitoringApi(FluentNHibernateJobStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
        }

        public IEnumerable<string> GetQueues()
        {
            lock (Mutex)
            {
                if (_queuesCache.Count == 0 || _cacheUpdated.Add(QueuesCacheTimeout) < DateTime.UtcNow)
                {
                    var result = _storage.UseSession(session =>
                    {
                        return session.Query<_JobQueue>().Select(i => i.Queue).Distinct().ToList();
                    }, FluentNHibernateJobStorageSessionStateEnum.Stateless);

                    _queuesCache = result;
                    _cacheUpdated = DateTime.UtcNow;
                }

                return _queuesCache.ToList();
            }
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            return _storage.UseSession(session =>
            {
                return session.Query<_JobQueue>().OrderBy(i => i.Id).Where(i => i.Queue == queue)
                    .Select(i => i.Job.Id).Skip(@from)
                    .Take(perPage).ToList();
            }, FluentNHibernateJobStorageSessionStateEnum.Stateless);
        }


        public IEnumerable<int> GetFetchedJobIds(string queue, int from, int perPage)
        {
            return Enumerable.Empty<int>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            return _storage.UseSession(session =>
            {
                var result = session.Query<_JobQueue>().Count(i => i.Queue == queue);

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = result
                };
            }, FluentNHibernateJobStorageSessionStateEnum.Stateless);
        }
    }
}