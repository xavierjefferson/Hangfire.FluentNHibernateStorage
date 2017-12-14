using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class ExpirationManager : IServerComponent
    {
        private const string DistributedLockKey = "expirationmanager";
        private const int NumberOfRecordsInSinglePass = 1000;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);

        private readonly TimeSpan _checkInterval;

        private readonly FluentNHibernateStorage _storage;

        public ExpirationManager(FluentNHibernateStorage storage)
            : this(storage, TimeSpan.FromHours(1))
        {
        }

        public ExpirationManager(FluentNHibernateStorage storage, TimeSpan checkInterval)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
            _checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Execute<_AggregatedCounter>(cancellationToken);
            Execute<_Job>(cancellationToken);
            Execute<_List>(cancellationToken);
            Execute<_Set>(cancellationToken);
            Execute<_Hash>(cancellationToken);

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        private void Execute<T>(CancellationToken cancellationToken) where T : IExpireWithId
        {
            var entityName = typeof(T).Name;
            Logger.DebugFormat("Removing outdated records from table '{0}'...", entityName);

            long removedCount = 0;

            do
            {
                try
                {
                    Logger.DebugFormat("delete from `{0}` where ExpireAt < @now limit @count;", entityName);

                    using (var distributedLock =
                        new FluentNHibernateDistributedLock(_storage, DistributedLockKey, DefaultLockTimeout,
                            cancellationToken).Acquire())
                    {
                        var idList = distributedLock.Session.Query<T>().Where(i => i.ExpireAt < DateTime.UtcNow)
                            .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                        removedCount = distributedLock.Session.DeleteById<T>(idList);
                    }

                    Logger.DebugFormat("removed records count={0}", removedCount);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }


                if (removedCount > 0)
                {
                    Logger.Trace(string.Format("Removed {0} outdated record(s) from '{1}' table.", removedCount,
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