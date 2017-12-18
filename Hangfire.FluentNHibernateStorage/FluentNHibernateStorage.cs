using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Hangfire.Annotations;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Maps;
using Hangfire.FluentNHibernateStorage.Monitoring;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorage : JobStorage, IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(FluentNHibernateStorage));

        private static readonly object Mutex = new object();
        private readonly CountersAggregator _countersAggregator;

        private readonly ExpirationManager _expirationManager;
        private readonly FluentNHibernateStorageOptions _options;

        private readonly Dictionary<IPersistenceConfigurer, ISessionFactory> _sessionFactories =
            new Dictionary<IPersistenceConfigurer, ISessionFactory>();

        protected readonly IPersistenceConfigurer PersistenceConfigurer;


        public FluentNHibernateStorage(IPersistenceConfigurer persistenceConfigurer)
            : this(persistenceConfigurer, new FluentNHibernateStorageOptions())
        {
        }

        public FluentNHibernateStorage(IPersistenceConfigurer persistenceConfigurer, FluentNHibernateStorageOptions options)
        {
           
            PersistenceConfigurer = persistenceConfigurer;

            _options = options ?? new FluentNHibernateStorageOptions();

            InitializeQueueProviders();
            _expirationManager = new ExpirationManager(this, _options.JobExpirationCheckInterval);
            _countersAggregator = new CountersAggregator(this, _options.CountersAggregateInterval);

           
        }

        internal virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public Func<IPersistenceConfigurer> ConfigurerFunc { get; set; }

        public void Dispose()
        {
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
            return new FluentNHibernateStorageConnection(this);
        }

        internal void UseTransactionStateless([InstantHandle] Action<IWrappedSession> action)
        {
            UseTransactionStateless(session =>
            {
                action(session);
                return true;
            }, null);
        }

        internal T UseTransactionStateless<T>(
            [InstantHandle] Func<IWrappedSession, T> func, IsolationLevel? isolationLevel)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseStatelessSession(func);
                transaction.Complete();

                return result;
            }
        }

        internal void UseStatefulTransaction([InstantHandle] Action<IWrappedSession> action)
        {
            UseStatefulTransaction(session =>
            {
                action(session);
                return true;
            }, null);
        }

        internal T UseStatefulTransaction<T>(
            [InstantHandle] Func<IWrappedSession, T> func, IsolationLevel? isolationLevel)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseSession(func);
                transaction.Complete();

                return result;
            }
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

        internal void UseStatelessSession([InstantHandle] Action<IWrappedSession> action)
        {
            using (var session = GetStatelessSession())
            {
                action(session);
            }
        }

        internal T UseStatelessSession<T>([InstantHandle] Func<IWrappedSession, T> func)
        {
            using (var session = GetStatelessSession())
            {
                var result = func(session);

                return result;
            }
        }

        internal void UseSession([InstantHandle] Action<IWrappedSession> action)
        {
            using (var session = GetStatefulSession())
            {
                action(session);
            }
        }

        internal T UseSession<T>([InstantHandle] Func<IWrappedSession, T> func)
        {
            using (var session = GetStatefulSession())
            {
                return func(session);
            }
        }

        private ISessionFactory GetSessionFactory(IPersistenceConfigurer configurer)
        {
            lock (Mutex)
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

        internal IWrappedSession GetStatefulSession()
        {
            lock (Mutex)
            {
                if (_options.PrepareSchemaIfNecessary)
                {
                    TryBuildSchema();
                }
            }
            return new SessionWrapper(GetSessionFactory(PersistenceConfigurer).OpenSession(), PersistenceConfigurer);
        }

        internal IWrappedSession GetStatelessSession()
        {
            lock (Mutex)
            {
                if (_options.PrepareSchemaIfNecessary)
                {
                    TryBuildSchema();
                }
            }
            return new StatelessSessionWrapper(GetSessionFactory(PersistenceConfigurer).OpenStatelessSession(), PersistenceConfigurer);
        }

        private void TryBuildSchema()
        {
            lock (Mutex)
            {
                Logger.Info("Start installing Hangfire SQL object check...");
                Fluently.Configure().Mappings(i => i.FluentMappings.AddFromAssemblyOf<_Hash>())
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