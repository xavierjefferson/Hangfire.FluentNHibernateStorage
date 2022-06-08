using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public interface IDatabaseFixture 
    {
        IPersistenceConfigurer GetPersistenceConfigurer();
        void Cleanup();
        FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null);
        void EnsurePersistenceConfigurer();
    }
}