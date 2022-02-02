using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class ExpirationManager : IBackgroundProcess, IServerComponent
    {
        // This value should be high enough to optimize the deletion as much, as possible,
        // reducing the number of queries. But low enough to cause lock escalations (it
        // appears, when ~5000 locks were taken, but this number is a subject of version).
        // Note, that lock escalation may also happen during the cascade deletions for
        // State (3-5 rows/job usually) and JobParameters (2-3 rows/job usually) tables.
        private const int DefaultNumberOfRecordsInSinglePass = 1000;

        internal const string DistributedLockKey = "locks:expirationmanager";

        private static readonly ILog Logger = LogProvider.For<ExpirationManager>();

        internal static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(5);
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

        private class DeletionArgs
        {
            public CancellationToken CancellationToken { get; set; }
            public int NumberOfRecordsInSinglePass { get; set; }
        }


        public void Execute(CancellationToken cancellationToken)
        {
            var numberOfRecordsInSinglePass = Storage.Options.DeleteExpiredBatchSize;
            if (numberOfRecordsInSinglePass <= 0 || numberOfRecordsInSinglePass > 100_000)
                numberOfRecordsInSinglePass = DefaultNumberOfRecordsInSinglePass;
            var deletionArgs = new DeletionArgs
            {
                NumberOfRecordsInSinglePass = numberOfRecordsInSinglePass, CancellationToken = cancellationToken
            };


            var actions = new List<Action<DeletionArgs>>
            {
                DeleteJobItems, DeleteListItems, DeleteAggregatedCounterItems, DeleteSetItems, DeleteHashItems,
                DeleteDistributedLockItems
            };
            foreach (var action in actions)
                try
                {
                    action(deletionArgs);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error while deleting expired items", ex);
                }

            cancellationToken.Wait(Storage.Options.JobExpirationCheckInterval);
        }

        private void DeleteDistributedLockItems(DeletionArgs deletionArgs)
        {
            WithLock(deletionArgs, nameof(_DistributedLock), () =>
            {
                return DeleteEntities<_DistributedLock>((session, cutoff) =>
                {
                    var unixDate = cutoff.ToEpochDate();
                    var idList = session.Query<_DistributedLock>()
                        .Where(i => i.ExpireAtAsLong < unixDate).Take(deletionArgs.NumberOfRecordsInSinglePass)
                        .Select(i => i.Id)
                        .ToList();
                    return session.DeleteByInt32Id<_DistributedLock>(idList);
                });
            });
        }

        private void DeleteHashItems(DeletionArgs deletionArgs)
        {
            DeleteExpirableEntityWithLock<_Hash>(deletionArgs);
        }

        private void DeleteSetItems(DeletionArgs deletionArgs)
        {
            DeleteExpirableEntityWithLock<_Set>(deletionArgs);
        }

        private void DeleteAggregatedCounterItems(DeletionArgs deletionArgs)
        {
            DeleteExpirableEntityWithLock<_AggregatedCounter>(deletionArgs);
        }

        private void DeleteListItems(DeletionArgs deletionArgs)
        {
            DeleteExpirableEntityWithLock<_List>(deletionArgs);
        }

        private void DeleteJobItems(DeletionArgs deletionArgs)
        {
            WithLock(deletionArgs, "JobItems", () =>
            {
                DeleteJobDetailEntity<_JobQueue>(deletionArgs);
                DeleteJobDetailEntity<_JobParameter>(deletionArgs);
                DeleteJobDetailEntity<_JobState>(deletionArgs);
                DeleteExpirableEntity<_Job>(deletionArgs);
                return 0;
            });
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
#pragma warning restore 618
        private void DeleteJobDetailEntity<T>(DeletionArgs info) where T : class, IJobChild, IInt32Id

        {
            DeleteEntities<T>((session, cutoff) =>
            {
                var idList = session.Query<T>().Where(i => i.Job.ExpireAt < cutoff)
                    .Take(info.NumberOfRecordsInSinglePass)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<T>(idList);
            });
        }

        private long DeleteExpirableEntity<T>(DeletionArgs info) where T : class, IExpirableWithId
        {
            return DeleteEntities<T>((session, cutoff) =>
            {
                var ids = session.Query<T>()
                    .Where(i => i.ExpireAt < cutoff).Take(info.NumberOfRecordsInSinglePass)
                    .Select(i => i.Id)
                    .ToList();
                return session.DeleteByInt32Id<T>(ids);
            });
        }

        private void DeleteExpirableEntityWithLock<T>(DeletionArgs deletionArgs)
            where T : class, IExpirableWithId
        {
            WithLock(deletionArgs, typeof(T).Name,
                () => DeleteExpirableEntity<T>(deletionArgs));
        }

        private void WithLock(DeletionArgs args, string subKey, Func<long> func)
        {
            while (!args.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var fluentNHibernateDistributedLock = FluentNHibernateDistributedLock.Acquire(Storage,
                        string.Format("{0}:{1}", DistributedLockKey, subKey),
                        DefaultLockTimeout,
                        args.CancellationToken);
                    if (fluentNHibernateDistributedLock != null)
                        using (fluentNHibernateDistributedLock)
                        {
                            var removedCount = func();
                            if (removedCount < args.NumberOfRecordsInSinglePass) break;
                        }
                }
                catch (DistributedLockTimeoutException)
                {
                    Logger.Debug("Distributed lock acquire timeout was exceeded");
                }

                args.CancellationToken.Wait(DelayBetweenPasses);
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