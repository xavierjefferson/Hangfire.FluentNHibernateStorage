using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage.Tests.Providers
{
    public interface IDbProvider1
    {
        void EnsurePersistenceConfigurer();
    }

    public interface IDbProvider : IDbProvider1
    {
        IPersistenceConfigurer GetPersistenceConfigurer();
        void Cleanup();
        FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null);
    }
}