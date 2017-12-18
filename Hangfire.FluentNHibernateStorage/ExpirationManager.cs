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
            var baseDate = DateTime.UtcNow;
            BatchDelete<_Job>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_Job>().Where(i => i.ExpireAt < baseDate && i.CurrentState != null)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                if (!idList.Any())
                {
                    return 0;
                }

                var r = session.CreateQuery(SQLHelper.UpdateJobCurrentStateStatement).SetParameter(SQLHelper.ValueParameterName, null)
                    .SetParameterList(SQLHelper.IdParameterName, idList).ExecuteUpdate();

                return r;
            }, baseDate);
            BatchDelete<_JobState>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_JobState>().Where(i => i.Job.ExpireAt < baseDate)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobState>(idList);
            }, baseDate);
            BatchDelete<_JobQueue>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_JobQueue>().Where(i => i.Job.ExpireAt < baseDate)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobState>(idList);
            }, baseDate);
            BatchDelete<_JobParameter>(cancellationToken, (session, baseDate2) =>
            {
                var idList = session.Query<_JobParameter>().Where(i => i.Job.ExpireAt < baseDate)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                return session.DeleteByInt32Id<_JobParameter>(idList);
            }, baseDate);
            BatchDelete<_AggregatedCounter>(cancellationToken, DeleteExpirableWithId<_AggregatedCounter>, baseDate);
            BatchDelete<_Job>(cancellationToken, DeleteExpirableWithId<_Job>, baseDate);
            BatchDelete<_List>(cancellationToken, DeleteExpirableWithId<_List>, baseDate);
            BatchDelete<_Set>(cancellationToken, DeleteExpirableWithId<_Set>, baseDate);
            BatchDelete<_Hash>(cancellationToken, DeleteExpirableWithId<_Hash>, baseDate);

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        private long DeleteExpirableWithId<T>(IWrappedSession session, DateTime baseDate) where T : IExpirableWithId

        {
            List<int> ids;
            if (typeof(T) == typeof(_Job))
            {
                ids = session.Query<_Job>().Where(i => i.ExpireAt < baseDate && i.CurrentState == null)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
            }
            else
                ids = session.Query<T>().Where(i => i.ExpireAt < baseDate)
                    .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
            return session.DeleteByInt32Id<T>(ids);
        }


        private void BatchDelete<T>(CancellationToken cancellationToken,
            Func<IWrappedSession, DateTime, long> deleteFunc, DateTime baseDate)

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
                        removedCount = deleteFunc(distributedLock.Session, baseDate);
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