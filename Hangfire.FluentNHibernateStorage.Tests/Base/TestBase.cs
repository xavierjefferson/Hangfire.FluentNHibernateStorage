using System;
using FluentNHibernate.Cfg.Db;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base
{
    public class TestBase<T, U> : IClassFixture<U>, IDisposable where T : IDbProvider, new() where U: TestDatabaseFixture
    {
        private readonly IDbProvider _provider;


        private bool _disposedValue;
        private FluentNHibernateJobStorage _storage;


        public TestBase()
        {
            _provider = new T();
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TestBase()
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

        protected Mock<FluentNHibernateJobStorage> CreateMock(IPersistenceConfigurer configurer)
        {
            return new Mock<FluentNHibernateJobStorage>(configurer, new FluentNHibernateStorageOptions());
        }


        public FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            return _storage ?? (_storage = _provider.GetStorage(options));
        }

        public IPersistenceConfigurer GetPersistenceConfigurer()
        {
            return _provider.GetPersistenceConfigurer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) _provider.Cleanup();
                // TODO: dispose managed state (managed objects)


                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public void WithCleanTables(FluentNHibernateJobStorage storage, Action<FluentNHibernateJobStorage> action)
        {
            action(storage);
            using (var session = storage.GetStatelessSession())
            {
                session.DeleteAll<_JobState>();
                session.DeleteAll<_JobParameter>();
                session.DeleteAll<_JobQueue>();
                session.DeleteAll<_Job>();
                session.DeleteAll<_Hash>();
                session.DeleteAll<_Set>();
                session.DeleteAll<_List>();
                session.DeleteAll<_DistributedLock>();
                session.DeleteAll<_AggregatedCounter>();
                session.DeleteAll<_Counter>();
                session.DeleteAll<_Server>();
            }
        }


        public void WithCleanTables(FluentNHibernateJobStorage storage,
            Action<StatelessSessionWrapper> action)
        {
            WithCleanTables(storage, s => { action(s.GetStatelessSession()); });
        }

        public void UseSession(Action<StatelessSessionWrapper> action,
            Action<FluentNHibernateJobStorage> beforeAction = null)
        {
            using (var storage = GetStorage())
            {
                WithCleanTables(storage, s =>
                {
                    beforeAction?.Invoke(s);
                    action(s.GetStatelessSession());
                });
            }
        }
    }
}