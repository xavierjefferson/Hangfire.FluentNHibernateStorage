using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
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
        private readonly  object _dateOffsetMutex = new object();
        private static readonly object SessionFactoryMutex = new object();
        private readonly CountersAggregator _countersAggregator;

        private readonly ExpirationManager _expirationManager;
        private readonly FluentNHibernateStorageOptions _options;

        private readonly ServerTimeSyncManager _serverTimeSyncManager;

        private readonly Dictionary<IPersistenceConfigurer, ISessionFactory> _sessionFactories =
            new Dictionary<IPersistenceConfigurer, ISessionFactory>();

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
            FluentNHibernateStorageOptions options, ProviderTypeEnum type)
        {
            Type = type;
            PersistenceConfigurer = persistenceConfigurer ?? throw new ArgumentNullException("persistenceConfigurer");
            _options = options ?? new FluentNHibernateStorageOptions();

            InitializeQueueProviders();
            _expirationManager = new ExpirationManager(this, _options.JobExpirationCheckInterval);
            _countersAggregator = new CountersAggregator(this, _options.CountersAggregateInterval);
            _serverTimeSyncManager = new ServerTimeSyncManager(this, TimeSpan.FromMinutes(5));
            EnsureDualHasOneRow();
            RefreshUtcOffset();
        }

        protected IPersistenceConfigurer PersistenceConfigurer { get; set; }

        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public Func<IPersistenceConfigurer> ConfigurerFunc { get; set; }

        public ProviderTypeEnum Type { get; } = ProviderTypeEnum.None;

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
                    switch (Type)
                    {
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
                                session.DeleteByInt32Id<_Dual>(
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
            if (config is JetDriverConfiguration  || config is SQLiteConfiguration || config is MsSqlCeConfiguration)
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


        public List<IBackgroundProcess> GetBackgroundProcesses()
        {
            return new List<IBackgroundProcess> {_expirationManager, _countersAggregator, _serverTimeSyncManager};
        }


        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for SQL Server job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", _options.QueuePollInterval);
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

        internal T UseTransaction<T>([InstantHandle] Func<SessionWrapper, T> func, IsolationLevel? isolationLevel)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseSession(func);
                transaction.Complete();
                return result;
            }
        }

        internal void UseTransaction([InstantHandle] Action<SessionWrapper> action)
        {
            UseTransaction(session =>
            {
                action(session);
                return true;
            }, null);
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

        private ISessionFactory GetSessionFactory(IPersistenceConfigurer configurer)
        {
            lock (SessionFactoryMutex)
            {
                //SINGLETON!
                if (_sessionFactories.ContainsKey(configurer) && _sessionFactories[configurer] != null)
                {
                    return _sessionFactories[configurer];
                }

                var fluentConfiguration =
                    Fluently.Configure().Mappings(i => i.FluentMappings.AddFromAssemblyOf<_Hash>());

                _sessionFactories[configurer] = fluentConfiguration
                    .Database(configurer)
                    .BuildSessionFactory();
                return _sessionFactories[configurer];
            }
        }


        public SessionWrapper GetSession()
        {
            lock (SessionFactoryMutex)
            {
                if (_options.PrepareSchemaIfNecessary)
                {
                    TryBuildSchema();
                }
            }
            return new SessionWrapper(GetSessionFactory(PersistenceConfigurer).OpenSession(), Type, this);
        }


        private void TryBuildSchema()
        {
            lock (SessionFactoryMutex)
            {
                Logger.Info("Start installing Hangfire SQL object check...");
                Fluently.Configure()
                    .Mappings(i => i.FluentMappings.AddFromAssemblyOf<_Hash>())
                    .Database(PersistenceConfigurer)
                    .ExposeConfiguration(cfg =>
                    {
                        var schemaUpdate = new SchemaUpdate(cfg);
                        using (var stringWriter = new StringWriter())
                        {
                            string _last = null;
                            try
                            {
                                schemaUpdate.Execute(i =>
                                {
                                    _last = i;
                                    stringWriter.WriteLine(i);
                                }, true);
                            }
                            catch (Exception ex)
                            {
                                Logger.ErrorException(string.Format("Can't do schema update '{0}'", _last), ex);
                                throw;
                            }
                        }
                    })
                    .BuildConfiguration();

                Logger.Info("Hangfire SQL object check done.");
                _options.PrepareSchemaIfNecessary = false;
            }
        }
    }
}