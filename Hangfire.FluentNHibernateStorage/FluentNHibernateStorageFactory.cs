using FluentNHibernate.Cfg.Db;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        /// <summary>
        ///     Factory method.  Return a job storage provider based on the given provider type, connection string, and options
        /// </summary>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        /// <param name="providerType">Provider type from enumeration</param>
        /// <param name="options">Advanced options</param>
        public static FluentNHibernateJobStorage For(ProviderTypeEnum providerType, string nameOrConnectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateJobStorage(providerType, nameOrConnectionString, options);
        }


        /// <summary>
        ///     Return an NHibernate persistence configurerTells the bootstrapper to use a FluentNHibernate provider as a job
        ///     storage,
        ///     that can be accessed using the given connection string or
        ///     its name.
        /// </summary>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        /// <param name="providerType">Provider type from enumeration</param>
        /// <param name="options">Advanced options</param>
        public static IPersistenceConfigurer GetPersistenceConfigurer(ProviderTypeEnum providerType,
            string nameOrConnectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return FluentNHibernatePersistenceBuilder.GetPersistenceConfigurer(providerType,
                nameOrConnectionString, options);
        }
    }
}