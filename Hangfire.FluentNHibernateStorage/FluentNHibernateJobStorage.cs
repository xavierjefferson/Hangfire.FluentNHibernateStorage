using System;
using System.Collections;
using System.Collections.Generic;
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

        private static readonly object mutex = new object();
        private readonly CountersAggregator _countersAggregator;

        private readonly ExpirationManager _expirationManager;
        private readonly FluentNHibernateStorageOptions _options;

        private readonly Dictionary<IPersistenceConfigurer, ISessionFactory> _sessionFactories =
            new Dictionary<IPersistenceConfigurer, ISessionFactory>();


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
            PersistenceConfigurer = persistenceConfigurer;


            _options = options ?? new FluentNHibernateStorageOptions();

            InitializeQueueProviders();
            _expirationManager = new ExpirationManager(this, _options.JobExpirationCheckInterval);
            _countersAggregator = new CountersAggregator(this, _options.CountersAggregateInterval);

            EnsureDualHasOneRow();
        }

        protected IPersistenceConfigurer PersistenceConfigurer { get; set; }


        internal virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public Func<IPersistenceConfigurer> ConfigurerFunc { get; set; }

        public ProviderTypeEnum Type { get; } = ProviderTypeEnum.None;

        public void Dispose()
        {
        }

        private void EnsureDualHasOneRow()
        {
            try
            {
                using (var session = GetStatelessSession())
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
            if (config is MsSqlCeConfiguration)
            {
                return ProviderTypeEnum.MsSqlCeStandard;
            }
            if (config is MsSqlConfiguration)
            {
                return ProviderTypeEnum.MsSql2000;
            }
            if (config is PostgreSQLConfiguration)
            {
                return ProviderTypeEnum.PostgreSQLStandard;
            }
            if (config is JetDriverConfiguration)
            {
                throw new ArgumentException("Jet driver is explicitly not supported.");
            }
            if (config is DB2Configuration)
            {
                return ProviderTypeEnum.DB2Standard;
            }
            if (config is OracleClientConfiguration)
            {
                return ProviderTypeEnum.OracleClient9;
            }
            if (config is SQLiteConfiguration)
            {
                return ProviderTypeEnum.SQLite;
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
            return new List<IBackgroundProcess> {_expirationManager, _countersAggregator};
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
            using (var a = GetStatelessSession())
            {
                return a.CreateQuery(hql).List();
            }
        }

        internal T UseTransaction<T>(
            [InstantHandle] Func<IWrappedSession, T> func, IsolationLevel? isolationLevel,
            FluentNHibernateJobStorageSessionStateEnum state)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseSession(func,
                    state);
                transaction.Complete();

                return result;
            }
        }

        internal void UseTransaction([InstantHandle] Action<IWrappedSession> action,
            FluentNHibernateJobStorageSessionStateEnum state)
        {
            UseTransaction(session =>
                {
                    action(session);
                    return true;
                }, null,
                state);
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

        internal void UseSession([InstantHandle] Action<IWrappedSession> action,
            FluentNHibernateJobStorageSessionStateEnum state)
        {
            switch (state)
            {
                case FluentNHibernateJobStorageSessionStateEnum.Stateful:
                    using (var session = GetStatefulSession())
                    {
                        action(session);
                    }
                    break;
                default:
                    using (var session = GetStatelessSession())
                    {
                        action(session);
                    }
                    break;
            }
        }

        internal T UseSession<T>([InstantHandle] Func<IWrappedSession, T> func,
            FluentNHibernateJobStorageSessionStateEnum state)
        {
            switch (state)
            {
                case FluentNHibernateJobStorageSessionStateEnum.Stateful:
                    using (var session = GetStatefulSession())
                    {
                        var result = func(session);
                        return result;
                    }
                default:
                    using (var session = GetStatelessSession())
                    {
                        var result = func(session);
                        return result;
                    }
            }
        }

        private ISessionFactory GetSessionFactory(IPersistenceConfigurer configurer)
        {
            lock (mutex)
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


        public IWrappedSession GetStatefulSession()
        {
            lock (mutex)
            {
                if (_options.PrepareSchemaIfNecessary)
                {
                    TryBuildSchema();
                }
            }
            return new StatefulSessionWrapper(GetSessionFactory(PersistenceConfigurer).OpenSession(), Type);
        }

        public IWrappedSession GetStatelessSession()
        {
            lock (mutex)
            {
                if (_options.PrepareSchemaIfNecessary)
                {
                    TryBuildSchema();
                }
            }
            return new StatelessSessionWrapper(GetSessionFactory(PersistenceConfigurer).OpenStatelessSession(), Type);
        }

        private void TryBuildSchema()
        {
            lock (mutex)
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

    public enum FluentNHibernateJobStorageSessionStateEnum
    {
        Stateful,
        Stateless
    }
}