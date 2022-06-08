using System;
using System.IO;
using System.Reflection;
using FluentNHibernate.Cfg.Db;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public abstract class TestDatabaseFixture : IDisposable
    {
        protected IPersistenceConfigurer PersistenceConfigurer;

        protected static string Instance => Guid.NewGuid().ToString();


        public void Dispose()
        {
            OnDispose();
        }

        public abstract void EnsurePersistenceConfigurer();

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

        public IPersistenceConfigurer GetPersistenceConfigurer()
        {
            EnsurePersistenceConfigurer();
            return PersistenceConfigurer;
        }

        protected string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, Instance);
        }

        public abstract void CreateDatabase();

        public FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            EnsurePersistenceConfigurer();
            return new FluentNHibernateJobStorage(PersistenceConfigurer, options);
        }

        public abstract void OnDispose();

        // public abstract void CreateDatabase();
        //   public abstract void DestroyDatabase();

        public abstract IDatabaseFixture GetProvider();
    }
}