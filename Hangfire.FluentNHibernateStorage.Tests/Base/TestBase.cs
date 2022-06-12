using System;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Moq;

namespace Hangfire.FluentNHibernateStorage.Tests.Base
{
    public abstract class TestBase : IDisposable
    {
        private bool _disposedValue;
        private FluentNHibernateJobStorage _storage;

        protected TestBase(DatabaseFixtureBase fixture)
        {
            Fixture = fixture;
        }

        protected DatabaseFixtureBase Fixture { get; }

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


        public virtual FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            if (_storage == null)
            {
                _storage = Fixture.GetStorage(options);

                return _storage;
            }

            return _storage;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // CleanTables(GetStorage());}
                    // TODO: dispose managed state (managed objects)
                }


                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }


        protected void UseJobStorageConnectionWithSession(
            Action<StatelessSessionWrapper, FluentNHibernateJobStorageConnection> action)
        {
            UseJobStorageConnection(jobStorageConnection =>
            {
                jobStorageConnection.Storage.UseStatelessSession(s => action(s, jobStorageConnection));
            });
        }

        protected void UseJobStorageConnection(Action<FluentNHibernateJobStorageConnection> action,
            bool cleanTables = true, FluentNHibernateStorageOptions options = null)
        {
            var fluentNHibernateJobStorage = GetStorage(options);
            if (cleanTables)
                Fixture.CleanTables(fluentNHibernateJobStorage.GetStatelessSession());
            using (var jobStorage = new FluentNHibernateJobStorageConnection(fluentNHibernateJobStorage))
            {
                action(jobStorage);
            }
        }

        protected FluentNHibernateJobStorage GetStorageMock(
            Func<Mock<FluentNHibernateJobStorage>, FluentNHibernateJobStorage> func,
            FluentNHibernateStorageOptions options = null)
        {
            return Fixture.GetStorageMock(func, options);
        }

        public static _Job InsertNewJob(StatelessSessionWrapper session, Action<_Job> action = null)
        {
            var newJob = new _Job
            {
                InvocationData = string.Empty,
                Arguments = string.Empty,
                CreatedAt = session.Storage.UtcNow
            };
            action?.Invoke(newJob);
            session.Insert(newJob);

            return newJob;
        }
    }
}