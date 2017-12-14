using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using MySql.Data.MySqlClient;
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
            Ex<_AggregatedCounter>(cancellationToken);
            Ex<_Job>(cancellationToken);
            Ex<_List>(cancellationToken);
            Ex<_Set>(cancellationToken);
            Ex<_Hash>(cancellationToken);


            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        private void Ex<T>(CancellationToken cancellationToken) where T : IExpireWithId
        {
            Logger.DebugFormat("Removing outdated records from table '{0}'...", nameof(T));

            var removedCount = 0;

            do
            {
                _storage.UseConnection(connection =>
                {
                    try
                    {
                        Logger.DebugFormat("delete from `{0}` where ExpireAt < @now limit @count;", nameof(T));

                        using (
                            new FluentNHibernateDistributedLock(
                                connection,
                                DistributedLockKey,
                                DefaultLockTimeout,
                                cancellationToken).Acquire())
                        {
                            var p = connection.Query<T>().Where(i => i.ExpireAt < DateTime.UtcNow)
                                .Take(NumberOfRecordsInSinglePass).Select(i => i.Id).ToList();
                            var ss = string.Format("delete from {0} where {1} in (:ids)", typeof(T),
                                nameof(IExpireWithId.Id));
                            removedCount = connection.CreateQuery(ss).SetParameterList("ids", p).ExecuteUpdate();
                        }

                        Logger.DebugFormat("removed records count={0}", removedCount);
                    }
                    catch (MySqlException ex)
                    {
                        Logger.Error(ex.ToString());
                    }
                });

                if (removedCount > 0)
                {
                    Logger.Trace(string.Format("Removed {0} outdated record(s) from '{1}' table.", removedCount,
                        nameof(T)));

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