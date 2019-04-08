using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;

namespace Hangfire.FluentNHibernateStorage
{
    internal class ExpirationActionQueue : Queue<Action>
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CancellationToken _cancellationToken;
        private readonly DateTime _cutOffDate;
        private readonly FluentNHibernateJobStorage _storage;

        public ExpirationActionQueue(FluentNHibernateJobStorage storage, DateTime cutOffDate,
            CancellationToken cancellationToken1)
        {
            _storage = storage;
            _cancellationToken = cancellationToken1;
            _cutOffDate = cutOffDate;
        }

        public void Run()
        {
            var actionQueue = this;
            foreach (var action in actionQueue)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                action();
            }
        }

        public void EnqueueDeleteJobDetailEntity<T>() where T : IJobChild, IInt32Id

        {
            EnqueueDeleteEntities<T>((session, cutoff) =>
            {
                var idList = session.Query<T>()
                    .Where(i => i.Job.ExpireAt < cutoff)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<T>(idList);
            });
        }

        internal void EnqueueDeleteExpirableEntity<T>() where T : IExpirableWithId
        {
            EnqueueDeleteEntities<T>((session, cutoff) => session.DeleteExpirableWithId<T>(cutoff));
        }

        public void EnqueueDeleteEntities<T>(Func<SessionWrapper, DateTime, long> deleteFunc)

        {
            Enqueue(() =>
            {
                try
                {
                    var entityName = typeof(T).Name;
                    Logger.InfoFormat("Removing outdated records from table '{0}'...", entityName);

                    long removedCount = 0;

                    while (true)
                    {
                        try
                        {
                            using (new FluentNHibernateDistributedLock(_storage, ExpirationManager.DistributedLockKey,
                                ExpirationManager.DefaultLockTimeout,
                                _cancellationToken).Acquire())
                            {
                                using (var session = _storage.GetSession())
                                {
                                    removedCount = deleteFunc(session, _cutOffDate);
                                }
                            }

                            Logger.InfoFormat("removed records count={0}", removedCount);
                        }
                        catch (FluentNHibernateDistributedLockException ex)
                        {
                            Logger.Warn(string.Concat("Can't delete : ", ex.ToString()));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.ToString());
                        }


                        if (removedCount <= 0)
                        {
                            break;
                        }

                        Logger.Info(string.Format("Removed {0} outdated record(s) from '{1}' table.", removedCount,
                            entityName));

                        _cancellationToken.WaitHandle.WaitOne(ExpirationManager.DelayBetweenPasses);
                        _cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception) when (_cancellationToken.IsCancellationRequested)
                {
                    Logger.Warn("Cancellation was requested");
                }
            });
        }
    }
}