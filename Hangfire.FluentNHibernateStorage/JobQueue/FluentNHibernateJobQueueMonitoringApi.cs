using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    public class FluentNHibernateJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private static readonly TimeSpan QueuesCacheTimeout = TimeSpan.FromSeconds(5);

        private readonly FluentNHibernateJobStorage _storage;
        private readonly object Mutex = new object();
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
                if (_queuesCache.Count == 0 || _cacheUpdated.Add(QueuesCacheTimeout) < _storage.UtcNow)
                {
                    var result = _storage.UseSession(
                        session => { return session.Query<_JobQueue>().Select(i => i.Queue).Distinct().ToList(); });

                    _queuesCache = result;
                    _cacheUpdated = _storage.UtcNow;
                }

                return _queuesCache.ToList();
            }
        }

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            return _storage.UseSession(session =>
            {
                return session.Query<_JobQueue>()
                    .OrderBy(i => i.Id)
                    .Where(i => i.Queue == queue)
                    .Select(i => i.Job.Id)
                    .Skip(from)
                    .Take(perPage)
                    .ToList();
            });
        }


        public IEnumerable<long> GetFetchedJobIds(string queue, int from, int perPage)
        {
            //return Enumerable.Empty<long>();
            return _storage.UseSession(session =>
            {
                return session.Query<_JobQueue>()
                    .Where(i => i.FetchedAt != null & i.Queue == queue)
                    .OrderBy(i => i.Id)
                    .Skip(from)
                    .Take(perPage)
                    .Select(i => i.Id)
                    .ToList();
            });
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            return _storage.UseSession(session =>
            {
                var result = session.Query<_JobQueue>().Where(i => i.Queue == queue).Select(i => i.FetchedAt).ToList();

                return new EnqueuedAndFetchedCountDto
                {
                    EnqueuedCount = result.Count,
                    FetchedCount = result.Count(i => i != null)
                };
            });
        }
    }
}