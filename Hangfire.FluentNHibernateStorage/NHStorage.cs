using System;
using System.Collections.Generic;
using System.IO;
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
    public class NHStorage : JobStorage, IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(NHStorage));


        private readonly ISession _existingSession;
        private readonly NHStorageOptions _options;

        private readonly Dictionary<IPersistenceConfigurer, ISessionFactory> _sessionFactories =
            new Dictionary<IPersistenceConfigurer, ISessionFactory>();

        protected IPersistenceConfigurer _configurer;

        private bool _testBuild;

        public Action<MappingConfiguration> GetMappings;

        public NHStorage(IPersistenceConfigurer pcf)
            : this(pcf, new NHStorageOptions())
        {
        }

        public NHStorage(IPersistenceConfigurer pcf, NHStorageOptions options)
        {
            ConfigurerFunc = () => { return pcf; };


            _options = options ?? throw new ArgumentNullException("options");

             

            InitializeQueueProviders();
        }

        internal NHStorage(ISession existingSession)
        {
            _existingSession = existingSession ?? throw new ArgumentNullException("existingSession");
            _options = new NHStorageOptions();

            InitializeQueueProviders();
        }

        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public Func<IPersistenceConfigurer> ConfigurerFunc { get; set; }

        public void Dispose()
        {
        }


        private void InitializeQueueProviders()
        {
            QueueProviders =
                new PersistentJobQueueProviderCollection(
                    new NHJobQueueProvider(this, _options));
        }

        public override IEnumerable<IServerComponent> GetComponents()
        {
            yield return new ExpirationManager(this, _options.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _options.CountersAggregateInterval);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for SQL Server job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", _options.QueuePollInterval);
        }


        public override IMonitoringApi GetMonitoringApi()
        {
            return new NHMonitoringApi(this, _options.DashboardJobListLimit);
        }

        public override IStorageConnection GetConnection()
        {
            return new NHStorageConnection(this);
        }


        internal void UseTransaction([InstantHandle] Action<ISession> action)
        {
            UseTransaction(session =>
            {
                action(session);
                return true;
            }, null);
        }

        internal T UseTransaction<T>(
            [InstantHandle] Func<ISession, T> func, IsolationLevel? isolationLevel)
        {
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseConnection(func);
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

        internal void UseConnection([InstantHandle] Action<ISession> action)
        {
            UseConnection(session =>
            {
                action(session);
                return true;
            });
        }

        internal T UseConnection<T>([InstantHandle] Func<ISession, T> func)
        {
            ISession session = null;

            try
            {
                session = CreateAndOpenSession();
                return func(session);
            }
            finally
            {
                ReleaseConnection(session);
            }
        }

        internal ISession CreateAndOpenSession()
        {
            if (_existingSession != null)
            {
                return _existingSession;
            }

            var connection = GetSession();

            return connection;
        }

        internal void ReleaseConnection(ISession session)
        {
            if (session != null && !ReferenceEquals(session, _existingSession))
            {
                session.Dispose();
            }
        }

        public void BuildSchemaIfNeeded(IPersistenceConfigurer configurer)
        {
            var mappings = GetMappings;
            var fluentConfiguration = Fluently.Configure()
                .Database(configurer)
                .Mappings(GetMappings)
                .ExposeConfiguration(cfg =>
                {
                    var a = new SchemaUpdate(cfg);
                    using (var stringWriter = new StringWriter())
                    {
                        try
                        {
                            a.Execute(i => stringWriter.WriteLine(i), true);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                        var d = stringWriter.ToString();
                    }
                })
                .BuildConfiguration();
        }


        private ISessionFactory GetSessionFactory(IPersistenceConfigurer configurer)
        {
            //SINGLETON!
            if (_sessionFactories.ContainsKey(configurer) && _sessionFactories[configurer] != null)
            {
                return _sessionFactories[configurer];
            }

            var fluentConfiguration = Fluently.Configure().Mappings(i => i.FluentMappings.AddFromAssemblyOf<_Hash>());

            _sessionFactories[configurer] = fluentConfiguration
                .Database(configurer)
                .BuildSessionFactory();
            return _sessionFactories[configurer];
        }

        private IPersistenceConfigurer GetConfigurer()
        {
            if (_configurer == null)
            {
                _configurer = ConfigurerFunc();
            }
            return _configurer;
        }


        public ISession GetSession()
        {
            DoTestBuild();
            return GetSessionFactory(GetConfigurer()).OpenSession();
        }

        private void DoTestBuild()
        {
            if (_options.PrepareSchemaIfNecessary)
            {
                if (!_testBuild)
                {
                  Logger.Info("Start installing Hangfire SQL objects...");
                    BuildSchemaIfNeeded(GetConfigurer());
                    _testBuild = true; Logger.Info("Hangfire SQL objects installed.");
                }
            }
        }
    }
}