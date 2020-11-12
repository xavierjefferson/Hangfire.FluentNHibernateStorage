using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class ExpirationManager : IBackgroundProcess, IServerComponent
    {
        internal const string DistributedLockKey = "expirationmanager";

        private static readonly ILog Logger = LogProvider.For<ExpirationManager>();

        internal static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);

        protected readonly FluentNHibernateJobStorage Storage;


        public ExpirationManager(FluentNHibernateJobStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }


        public void Execute(BackgroundProcessContext context)
        {
            Execute(context.StoppedToken);
        }


        public void Execute(CancellationToken cancellationToken)
        {
            var actions = new List<Action<CancellationToken>>
            {
                DeleteJobItems, DeleteListItems, DeleteAggregatedCounterItems, DeleteSetItems, DeleteHashItems,
                DeleteDistributedLockItems
            };

            Parallel.ForEach(actions, action =>
            {
                try
                {
                    action(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error while deleting expired items", ex);
                }
            });

            cancellationToken.WaitHandle.WaitOne(Storage.Options.JobExpirationCheckInterval);
        }

        private void DeleteDistributedLockItems(CancellationToken cancellationToken)
        {
            WithLock(cancellationToken, nameof(_DistributedLock), () =>
            {
                return DeleteEntities<_DistributedLock>((session, cutoff) =>
                {
                    var unixDate = cutoff.ToEpochDate();
                    var idList = session.Query<_DistributedLock>()
                        .Where(i => i.ExpireAtAsLong < unixDate)
                        .Select(i => i.Id)
                        .ToList();
                    return session.DeleteByInt32Id<_DistributedLock>(idList);
                });
            });
        }

        private void DeleteHashItems(CancellationToken cancellationToken)
        {
            DeleteExpirableEntityWithLock<_Hash>(cancellationToken);
        }

        private void DeleteSetItems(CancellationToken cancellationToken)
        {
            DeleteExpirableEntityWithLock<_Set>(cancellationToken);
        }

        private void DeleteAggregatedCounterItems(CancellationToken cancellationToken)
        {
            DeleteExpirableEntityWithLock<_AggregatedCounter>(cancellationToken);
        }

        private void DeleteListItems(CancellationToken cancellationToken)
        {
            DeleteExpirableEntityWithLock<_List>(cancellationToken);
        }

        private void DeleteJobItems(CancellationToken cancellationToken)
        {
            WithLock(cancellationToken, "JobItems", () =>
            {
                DeleteJobDetailEntity<_JobQueue>();
                DeleteJobDetailEntity<_JobParameter>();
                DeleteJobDetailEntity<_JobState>();
                DeleteExpirableEntity<_Job>();
                return 0;
            });
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
        public void DeleteJobDetailEntity<T>() where T : class, IJobChild, IInt32Id

        {
            DeleteEntities<T>((session, cutoff) =>
            {
                var idList = session.Query<T>().Where(i => i.Job.ExpireAt < cutoff)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<T>(idList);
            });
        }

        internal long DeleteExpirableEntity<T>() where T : class, IExpirableWithId
        {
            return DeleteEntities<T>((session, cutoff) =>
            {
                var ids = session.Query<T>()
                    .Where(i => i.ExpireAt < cutoff)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<T>(ids);
            });
        }

        internal void DeleteExpirableEntityWithLock<T>(CancellationToken cancellationToken)
            where T : class, IExpirableWithId
        {
            WithLock(cancellationToken, typeof(T).Name, DeleteExpirableEntity<T>);
        }

        public void WithLock(CancellationToken cancellationToken, string subKey, Func<long> func)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var fluentNHibernateDistributedLock = new FluentNHibernateDistributedLock(Storage,
                    string.Format("{0}:{1}", DistributedLockKey, subKey),
                    DefaultLockTimeout,
                    cancellationToken).Acquire();
                if (fluentNHibernateDistributedLock != null)
                    using (fluentNHibernateDistributedLock)
                    {
                        var removedCount = func();
                        if (removedCount <= 0) break;
                    }

                cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
            }
        }

        public long DeleteEntities<T>(Func<StatelessSessionWrapper, DateTime, long> deleteFunc) where T : class

        {
            var entityName = Storage.GetTableName<T>();
            Logger.DebugFormat("Removing expired rows from table '{0}'", entityName);
            return Storage.UseStatelessSession(session =>
            {
                var removedCount = deleteFunc(session, Storage.UtcNow);
                Logger.DebugFormat("Removed {0} expired rows from table '{1}'", removedCount,
                    entityName);
                return removedCount;
            });
        }
    }
}