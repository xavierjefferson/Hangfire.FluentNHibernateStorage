using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public static class FluentNHibernateStorageBootstrapperConfigurationExtensions
    {
        /// <summary>
        ///     Tells the bootstrapper to use FluentNHibernate provider as a job storage,
        ///     that can be accessed using the given connection string or
        ///     its name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        /// <param name="providerType">Provider type from enumeration</param>
        /// <param name="options">Advanced options</param>
        public static FluentNHibernateJobStorage UseFluentNHibernateJobStorage(
            this IGlobalConfiguration configuration,
            string nameOrConnectionString, ProviderTypeEnum providerType, FluentNHibernateStorageOptions options = null)
        {
            var storage = FluentNHibernateStorageFactory.For(providerType, nameOrConnectionString,
                options ?? new FluentNHibernateStorageOptions());
            configuration.UseStorage(storage);
            return storage;
        }
    }
}