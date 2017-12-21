using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
    public class ExpirationManager : IBackgroundProcess
    {
        private const string DistributedLockKey = "expirationmanager";
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);

        private readonly TimeSpan _checkInterval;

        private readonly FluentNHibernateJobStorage _storage;

        public ExpirationManager(FluentNHibernateJobStorage storage)
            : this(storage, TimeSpan.FromHours(1))
        {
        }

        public ExpirationManager(FluentNHibernateJobStorage storage, TimeSpan checkInterval)
        {
            _storage = storage ?? throw new ArgumentNullException("storage");
            _checkInterval = checkInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var cancellationToken = context.CancellationToken;
        
            BatchDelete<_JobState>(cancellationToken, (session, baseDate2) =>
            {

                var idList = session.Query<_JobState>().Where(i => i.Job.ExpireAt < session.Storage.UtcNow)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobState>(idList);
            });
            BatchDelete<_JobQueue>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_JobQueue>().Where(i => i.Job.ExpireAt < session.Storage.UtcNow)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobState>(idList);
            });
            BatchDelete<_JobParameter>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_JobParameter>().Where(i => i.Job.ExpireAt < session.Storage.UtcNow)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobParameter>(idList);
            });
            BatchDelete<_DistributedLock>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_DistributedLock>().Where(i => i.ExpireAtAsLong < session.Storage.UtcNow.ToUnixDate())
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_DistributedLock>(idList);
            });
            BatchDelete<_AggregatedCounter>(cancellationToken, DeleteExpirableWithId<_AggregatedCounter>);
            BatchDelete<_Job>(cancellationToken, DeleteExpirableWithId<_Job>);
            BatchDelete<_List>(cancellationToken, DeleteExpirableWithId<_List>);
            BatchDelete<_Set>(cancellationToken, DeleteExpirableWithId<_Set>);
            BatchDelete<_Hash>(cancellationToken, DeleteExpirableWithId<_Hash>);

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        private long DeleteExpirableWithId<T>(IWrappedSession session, DateTime baseDate) where T : IExpirableWithId

        {
            List<int> ids;
                ids = session.Query<T>().Where(i => i.ExpireAt < baseDate)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
            return session.DeleteByInt32Id<T>(ids);
        }


        private void BatchDelete<T>(CancellationToken cancellationToken,
            Func<IWrappedSession, DateTime, long> deleteFunc)

        {
            var entityName = typeof(T).Name;
            Logger.InfoFormat("Removing outdated records from table '{0}'...", entityName);

            long removedCount = 0;

            do
            {
                try
                {
                    using (var distributedLock =
                        new FluentNHibernateStatelessDistributedLock(_storage, DistributedLockKey, DefaultLockTimeout,
                            cancellationToken).Acquire())
                    {
                        removedCount = deleteFunc(distributedLock.Session, _storage.UtcNow);
                    }

                    Logger.InfoFormat("removed records count={0}", removedCount);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }


                if (removedCount > 0)
                {
                    Logger.Info(string.Format("Removed {0} outdated record(s) from '{1}' table.", removedCount,
                        entityName));

                    cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (removedCount != 0);
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
    }
}