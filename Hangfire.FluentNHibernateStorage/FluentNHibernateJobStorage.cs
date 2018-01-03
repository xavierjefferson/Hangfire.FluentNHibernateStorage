using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Hangfire.Annotations;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Monitoring;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateJobStorage : JobStorage, IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(FluentNHibernateJobStorage));
        private static readonly object SessionFactoryMutex = new object();
        private readonly CountersAggregator _countersAggregator;
        private readonly object _dateOffsetMutex = new object();

        private readonly ExpirationManager _expirationManager;
        private readonly FluentNHibernateStorageOptions _options;

        private readonly ServerTimeSyncManager _serverTimeSyncManager;

        private ISessionFactory _sessionFactory;


        private TimeSpan _utcOffset = TimeSpan.Zero;


        public FluentNHibernateJobStorage(IPersistenceConfigurer persistenceConfigurer)
            : this(persistenceConfigurer, new FluentNHibernateStorageOptions())
        {
        }

        public FluentNHibernateJobStorage(IPersistenceConfigurer persistenceConfigurer,
            FluentNHibernateStorageOptions options) : this(persistenceConfigurer, options,
            InferProviderType(persistenceConfigurer))
        {
        }

        internal FluentNHibernateJobStorage(IPersistenceConfigurer persistenceConfigurer,
            FluentNHibernateStorageOptions options, ProviderTypeEnum providerType)
        {
            ProviderType = providerType;
            PersistenceConfigurer = persistenceConfigurer ?? throw new ArgumentNullException("persistenceConfigurer");
            _options = options ?? new FluentNHibernateStorageOptions();

            InitializeQueueProviders();
            _expirationManager = new ExpirationManager(this, _options.JobExpirationCheckInterval);
            _countersAggregator = new CountersAggregator(this, _options.CountersAggregateInterval);
            _serverTimeSyncManager = new ServerTimeSyncManager(this, TimeSpan.FromMinutes(5));

            //escalate session factory issues early
            try
            {
                var tmp = GetSessionFactory();
                EnsureDualHasOneRow();
                RefreshUtcOffset();
            }
            catch (FluentConfigurationException ex)
            {
                throw ex.InnerException??ex;
            }
        }

        protected IPersistenceConfigurer PersistenceConfigurer { get; set; }

        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public ProviderTypeEnum ProviderType { get; } = ProviderTypeEnum.None;

        public DateTime UtcNow
        {
            get
            {
                lock (_dateOffsetMutex)
                {
                    var utcNow = DateTime.UtcNow.Add(_utcOffset);
                    return utcNow;
                }
            }
        }

        public void Dispose()
        {
        }

        public void RefreshUtcOffset()
        {
            Logger.Debug("Refreshing UTC offset");
            lock (_dateOffsetMutex)
            {
                using (var session = GetSession())
                {
                    IQuery query;
                    switch (ProviderType)
                    {
                        case ProviderTypeEnum.OracleClient10Managed:
                        case ProviderTypeEnum.OracleClient9Managed:

                        case ProviderTypeEnum.OracleClient10:
                        case ProviderTypeEnum.OracleClient9:
                            query = session.CreateSqlQuery("select systimestamp at time zone 'UTC' from dual");
                            break;
                        case ProviderTypeEnum.PostgreSQLStandard:
                        case ProviderTypeEnum.PostgreSQL81:
                        case ProviderTypeEnum.PostgreSQL82:
                            query = session.CreateSqlQuery("SELECT NOW() at time zone 'utc'");
                            break;
                        case ProviderTypeEnum.MySQL:
                            query = session.CreateSqlQuery("select utc_timestamp()");
                            break;
                        case ProviderTypeEnum.MsSql2000:
                        case ProviderTypeEnum.MsSql2005:
                        case ProviderTypeEnum.MsSql2008:
                        case ProviderTypeEnum.MsSql2012:
                            query = session.CreateSqlQuery("select getutcdate()");
                            break;
                        default:
                            query =
                                session.CreateQuery(string.Format("select current_timestamp() from {0}",
                                    nameof(_Dual)));
                            break;
                    }
                    var stopwatch = new Stopwatch();
                    var current = DateTime.UtcNow;
                    stopwatch.Start();
                    var serverUtc = (DateTime) query.UniqueResult();
                    stopwatch.Stop();
                    _utcOffset = serverUtc.Subtract(current).Subtract(stopwatch.Elapsed);
                }
            }
        }

        private void EnsureDualHasOneRow()
        {
            try
            {
                using (var session = GetSession())
                {
                    using (var tx = session.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        var count = session.Query<_Dual>().Count();
                        switch (count)
                        {
                            case 1:
                                return;
                            case 0:
                                session.Insert(new _Dual {Id = 1});
                                session.Flush();
                                break;
                            default:
                                session.DeleteByInt64Id<_Dual>(
                                    session.Query<_Dual>().Skip(1).Select(i => i.Id).ToList());
                                break;
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WarnException("Issue with dual table", ex);
                throw;
            }
        }

        private static ProviderTypeEnum InferProviderType(IPersistenceConfigurer config)
        {
            if (config is MsSqlConfiguration)
            {
                return ProviderTypeEnum.MsSql2000;
            }
            if (config is PostgreSQLConfiguration)
            {
                return ProviderTypeEnum.PostgreSQLStandard;
            }
            if (config is JetDriverConfiguration || config is SQLiteConfiguration || config is MsSqlCeConfiguration)
            {
                throw new ArgumentException($"{config.GetType().Name} is explicitly not supported.");
            }
            if (config is DB2Configuration)
            {
                return ProviderTypeEnum.DB2Standard;
            }
            if (config is OracleClientConfiguration)
            {
                return ProviderTypeEnum.OracleClient9;
            }
            if (config is FirebirdConfiguration)
            {
                return ProviderTypeEnum.Firebird;
            }
            return ProviderTypeEnum.None;
        }


        private void InitializeQueueProviders()
        {
            QueueProviders =
                new PersistentJobQueueProviderCollection(
                    new FluentNHibernateJobQueueProvider(this, _options));
        }

#pragma warning disable 618
        public override IEnumerable<IServerComponent> GetComponents()

        {
            return new List<IServerComponent> {_expirationManager, _countersAggregator, _serverTimeSyncManager};
        }
#pragma warning restore 618

        public List<IBackgroundProcess> GetBackgroundProcesses()
        {
            return new List<IBackgroundProcess> {_expirationManager, _countersAggregator, _serverTimeSyncManager};
        }


        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", _options.QueuePollInterval);
            logger.InfoFormat("    Schema: {0}", _options.DefaultSchema??"(not specified)");
        }


        public override IMonitoringApi GetMonitoringApi()
        {
            return new FluentNHibernateMonitoringApi(this, _options.DashboardJobListLimit);
        }

        public override IStorageConnection GetConnection()
        {
            return new FluentNHibernateJobStorageConnection(this);
        }


        public IEnumerable ExecuteHqlQuery(string hql)
        {
            using (var session = GetSession())
            {
                return session.CreateQuery(hql).List();
            }
        }

        internal T UseTransaction<T>([InstantHandle] Func<SessionWrapper, T> func,
            IsolationLevel? isolationLevel = null)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseSession(func);
                transaction.Complete();
                return result;
            }
        }

        internal void UseTransaction([InstantHandle] Action<SessionWrapper> action,
            IsolationLevel? isolationLevel = null)
        {
            UseTransaction(session =>
            {
                action(session);
                return true;
            }, isolationLevel);
        }

        private TransactionScope CreateTransaction(IsolationLevel? isolationLevel)
        {
            return isolationLevel != null
                ? new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions
                    {
                        IsolationLevel = isolationLevel.Value,
                        Timeout = _options.TransactionTimeout
                    })
                : new TransactionScope();
        }

        public void ResetAll()
        {
            using (var session = GetSession())
            {
                session.DeleteAll<_List>();
                session.DeleteAll<_Hash>();
                session.DeleteAll<_Set>();
                session.DeleteAll<_Server>();
                session.DeleteAll<_JobQueue>();
                session.DeleteAll<_JobParameter>();
                session.DeleteAll<_JobState>();
                session.DeleteAll<_Job>();
                session.DeleteAll<_Counter>();
                session.DeleteAll<_AggregatedCounter>();
                session.DeleteAll<_DistributedLock>();
                session.Flush();
            }
        }

        public void UseSession([InstantHandle] Action<SessionWrapper> action)
        {
            using (var session = GetSession())
            {
                action(session);
            }
        }

        public T UseSession<T>([InstantHandle] Func<SessionWrapper, T> func)
        {
            using (var session = GetSession())
            {
                var result = func(session);
                return result;
            }
        }

        private ISessionFactory GetSessionFactory()
        {
            lock (SessionFactoryMutex)
            {
                if (_sessionFactory != null)
                {
                    return _sessionFactory;
                }

                var fluentConfiguration =
                    Fluently.Configure().Mappings(i => i.FluentMappings.AddFromAssemblyOf<_Hash>());

                _sessionFactory = fluentConfiguration
                    .Database(PersistenceConfigurer)
                    .ExposeConfiguration(cfg =>
                    {
                        if (!_options.PrepareSchemaIfNecessary)
                        {
                            return;
                        }
                        Logger.Info("Start schema check...");
                        var schemaUpdate = new SchemaUpdate(cfg);

                        string lastStatement = null;
                        try
                        {
                            schemaUpdate.Execute(i => { lastStatement = i; }, true);
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorException(string.Format("Can't do schema update '{0}'", lastStatement), ex);
                            throw;
                        }

                        _options.PrepareSchemaIfNecessary = false;
                        Logger.Info("Schema check done.");
                    })
                    .BuildSessionFactory();
                return _sessionFactory;
            }
        }


        public SessionWrapper GetSession()
        {
            return new SessionWrapper(GetSessionFactory().OpenSession(), this);
        }
    }
}