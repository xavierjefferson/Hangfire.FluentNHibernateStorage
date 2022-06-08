using System;
using System.IO;
using System.Reflection;
using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage.Tests.Providers
{
    public abstract class ProviderBase : IDbProvider1
    {
        protected IPersistenceConfigurer PersistenceConfigurer;

        protected static string Instance => Guid.NewGuid().ToString();

        public abstract void EnsurePersistenceConfigurer();

        public IPersistenceConfigurer GetPersistenceConfigurer()
        {
            EnsurePersistenceConfigurer();
            return PersistenceConfigurer;
        }

        protected string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name);
        }

        public abstract void CreateDatabase();
        public abstract void DestroyDatabase();

        public FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            EnsurePersistenceConfigurer();
            return new FluentNHibernateJobStorage(PersistenceConfigurer, options);
        }
    }
}