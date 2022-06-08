using System;
using FluentNHibernate.Cfg.Db;
using Moq;

namespace Hangfire.FluentNHibernateStorage.Tests.Base
{
    public abstract class TestBase : IDisposable
    {
        private readonly IDatabaseFixture _provider;


        private bool _disposedValue;
        private FluentNHibernateJobStorage _storage;

        protected TestBase(TestDatabaseFixture fixture)
        {
            Fixture = fixture;
            _provider = fixture.GetProvider();
        }

        protected TestDatabaseFixture Fixture { get; }

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
            if (_storage == null)
            {
                _storage = _provider.GetStorage(options);

                return _storage;
            }

            return _storage;
        }

        public IPersistenceConfigurer GetPersistenceConfigurer()
        {
            return _provider.GetPersistenceConfigurer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    CleanTables(GetStorage());
                // TODO: dispose managed state (managed objects)


                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        private void CleanTables(FluentNHibernateJobStorage storage)
        {
            using (var session = storage.GetStatelessSession())
            {
                CleanTables(session);
            }
        }

        private void CleanTables(StatelessSessionWrapper session)
        {
            Fixture.CleanTables(session);
        }


        public void UseSession(FluentNHibernateJobStorage storage,
            Action<StatelessSessionWrapper> action)
        {
            action(storage.GetStatelessSession());
        }

        public void UseNewSession(Action<StatelessSessionWrapper> action,
            Action<FluentNHibernateJobStorage> beforeAction = null)
        {
            //don't wrap in 'using' because we don't want to dispose yet
            var storage = GetStorage();

            beforeAction?.Invoke(storage);
            action(storage.GetStatelessSession());
        }
    }
}