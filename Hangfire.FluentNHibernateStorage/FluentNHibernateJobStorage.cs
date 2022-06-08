using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Metadata;
using NHibernate.Persister.Entity;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateJobStorage : JobStorage, IDisposable
    {
        private static readonly ILog Logger = LogProvider.For<FluentNHibernateJobStorage>();

        private CountersAggregator _countersAggregator;
        private ExpirationManager _expirationManager;
        private ServerTimeSyncManager _serverTimeSyncManager;
        private ISessionFactory _sessionFactory;
        private bool _disposedValue;

        public FluentNHibernateJobStorage(ProviderTypeEnum providerType, string nameOrConnectionString,
            FluentNHibernateStorageOptions options = null) : this(
            SessionFactoryBuilder.GetFromAssemblyOf<_CounterMap>(providerType, nameOrConnectionString,
                options ?? new FluentNHibernateStorageOptions()))
        {
        }


        public FluentNHibernateJobStorage(IPersistenceConfigurer persistenceConfigurer,
            FluentNHibernateStorageOptions options = null)
        {
            if (persistenceConfigurer == null) throw new ArgumentNullException(nameof(persistenceConfigurer));
            Initialize(SessionFactoryBuilder.GetFromAssemblyOf<_CounterMap>(
                persistenceConfigurer, options));
        }

        public FluentNHibernateJobStorage(SessionFactoryInfo info)
        {
            Initialize(info);
        }

        public void RefreshUtcOFfset()
        {
            using (var session = this.SessionFactoryInfo.SessionFactory.OpenSession())
            {
                this.UtcOffset = DateTime.UtcNow.Subtract(session.GetUtcNow(this.ProviderType));
            }
        }

        internal IDictionary<string, IClassMetadata> ClassMetadataDictionary { get; set; }

        internal TimeSpan UtcOffset { get; set; }
        internal SessionFactoryInfo SessionFactoryInfo { get; set; }

        public FluentNHibernateStorageOptions Options { get; set; }


        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public ProviderTypeEnum ProviderType { get; set; } = ProviderTypeEnum.None;

        public DateTime UtcNow => DateTime.UtcNow.Add(UtcOffset);


        private void Initialize(SessionFactoryInfo info)
        {
            SessionFactoryInfo = info ?? throw new ArgumentNullException(nameof(info));
            ClassMetadataDictionary = info.SessionFactory.GetAllClassMetadata();
            ProviderType = info.ProviderType;
            _sessionFactory = info.SessionFactory;

            var tmp = info.Options as FluentNHibernateStorageOptions;
            Options = tmp ?? new FluentNHibernateStorageOptions();

            InitializeQueueProviders();
            _expirationManager = new ExpirationManager(this);
            _countersAggregator = new CountersAggregator(this);
            _serverTimeSyncManager = new ServerTimeSyncManager(this);


            //escalate session factory issues early
            try
            {
                EnsureDualHasOneRow();
            }
            catch (FluentConfigurationException ex)
            {
                throw ex.InnerException ?? ex;
            }

            RefreshUtcOFfset();
        }


        internal string GetTableName<T>() where T : class
        {
            string entityName;
            var fullName = typeof(T).FullName;
            if (ClassMetadataDictionary.ContainsKey(fullName))
            {
                var classMetadata = ClassMetadataDictionary[fullName] as SingleTableEntityPersister;
                entityName = classMetadata == null ? typeof(T).Name : classMetadata.TableName;
            }
            else
            {
                entityName = typeof(T).Name;
            }

            return entityName;
        }

        private void EnsureDualHasOneRow()
        {
            try
            {
                UseStatelessSessionInTransaction(session =>
                {
                    var count = session.Query<_Dual>().Count();
                    switch (count)
                    {
                        case 1:
                            return;
                        case 0:
                            session.Insert(new _Dual { Id = 1 });
                            break;
                        default:
                            session.DeleteByInt32Id<_Dual>(
                                session.Query<_Dual>().Skip(1).Select(i => i.Id).ToList());
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.WarnException("Issue with dual table", ex);
                throw;
            }
        }

        private void InitializeQueueProviders()
        {
            QueueProviders =
                new PersistentJobQueueProviderCollection(
                    new FluentNHibernateJobQueueProvider(this));
        }


        public override void WriteOptionsToLog(ILog logger)
        {
            if (logger.IsInfoEnabled())
                logger.DebugFormat("Using the following options for job storage: {0}",
                    JsonConvert.SerializeObject(Options, Formatting.Indented));
        }


        public override IMonitoringApi GetMonitoringApi()
        {
            return new FluentNHibernateMonitoringApi(this);
        }

        public override IStorageConnection GetConnection()
        {
            return new FluentNHibernateJobStorageConnection(this);
        }


        internal T UseStatelessSessionInTransaction<T>([InstantHandle] Func<StatelessSessionWrapper, T> func)
        {
            using (var transaction = CreateTransaction())
            {
                var result = UseStatelessSession(func);
                transaction.Complete();
                return result;
            }
        }

        internal void UseStatelessSessionInTransaction([InstantHandle] Action<StatelessSessionWrapper> action)
        {
            UseStatelessSessionInTransaction(statelessSessionWrapper =>
            {
                action(statelessSessionWrapper);
                return false;
            });
        }


        public TransactionScope CreateTransaction()
        {
            return new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = Options.TransactionIsolationLevel,
                    Timeout = Options.TransactionTimeout
                });
        }


        public void UseStatelessSession([InstantHandle] Action<StatelessSessionWrapper> action)
        {
            using (var session = GetStatelessSession())
            {
                action(session);
            }
        }

        public T UseStatelessSession<T>([InstantHandle] Func<StatelessSessionWrapper, T> func)
        {
            using (var session = GetStatelessSession())
            {
                return func(session);
            }
        }

        public StatelessSessionWrapper GetStatelessSession()
        {
            var statelessSession = _sessionFactory.OpenStatelessSession();
            return new StatelessSessionWrapper(statelessSession, this);
        }

#pragma warning disable 618
        public List<IBackgroundProcess> GetBackgroundProcesses()
        {
            return new List<IBackgroundProcess> { _expirationManager, _countersAggregator, _serverTimeSyncManager };
        }

        public override IEnumerable<IServerComponent> GetComponents()

        {
            return new List<IServerComponent> { _expirationManager, _countersAggregator, _serverTimeSyncManager };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    if (_sessionFactory != null)
                    {
                        try
                        {
                            if (!_sessionFactory.IsClosed)
                                _sessionFactory.Close();
                        }
                        catch
                        {
                            // ignored
                        }

                        _sessionFactory.Dispose();
                        _sessionFactory = null;
                    }
                // TODO: dispose managed state (managed objects)

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FluentNHibernateJobStorage()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#pragma warning restore 618
    }
}