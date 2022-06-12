using System;
using System.IO;
using System.Reflection;
using FluentNHibernate.Cfg.Db;
using Hangfire.FluentNHibernateStorage.Entities;
using Moq;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures
{
    public abstract class DatabaseFixtureBase : IDisposable, IDatabaseFixture
    {
        private bool _updateSchema = true; //only create schema on the first pass
        protected IPersistenceConfigurer PersistenceConfigurer;

        protected static string Instance => Guid.NewGuid().ToString();

        public abstract void EnsurePersistenceConfigurer();

        public IPersistenceConfigurer GetPersistenceConfigurer()
        {
            EnsurePersistenceConfigurer();
            return PersistenceConfigurer;
        }

        public abstract void Cleanup();

        public FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            EnsurePersistenceConfigurer();
            options = ProgressOptions(options);
            var tmp = new FluentNHibernateJobStorage(PersistenceConfigurer, options);
            return tmp;
        }


        public void Dispose()
        {
            OnDispose();
        }

        protected void DeleteFolder(DirectoryInfo directoryInfo)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                }

            foreach (var info in directoryInfo.GetDirectories())
                try
                {
                    DeleteFolder(info);
                }
                catch
                {
                }

            directoryInfo.Delete();
        }

        public void CleanTables(StatelessSessionWrapper session)
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

        protected string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, Instance);
        }

        public abstract void CreateDatabase();

        private FluentNHibernateStorageOptions ProgressOptions(FluentNHibernateStorageOptions options = null)
        {
            if (options == null) options = new FluentNHibernateStorageOptions();
            options.UpdateSchema = _updateSchema;
            _updateSchema = false;
            return options;
        }

        public Mock<FluentNHibernateJobStorage> GetStorageMock(FluentNHibernateStorageOptions options = null)
        {
            return new Mock<FluentNHibernateJobStorage>(GetPersistenceConfigurer(), ProgressOptions(options));
        }

        public abstract void OnDispose();
    }
}