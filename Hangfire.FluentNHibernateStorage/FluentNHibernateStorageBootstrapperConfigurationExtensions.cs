using System;

namespace Hangfire.FluentNHibernateStorage
{
    public static class FluentNHibernateStorageBootstrapperConfigurationExtensions
    {
        /// <summary>
        ///     Tells the bootstrapper to use a FluentNHibernate provider as a job storage,
        ///     that can be accessed using the given connection string or
        ///     its name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="nameOrConnectionString">Connection string or its name</param>
        /// <param name="providerType">Provider type from enumeration</param>
        /// <param name="options">Advanced options</param>
        [Obsolete(
            "Please use `GlobalConfiguration.UseFluentNHibernateJobStorage` instead. Will be removed in version 2.0.0.")]
        public static FluentNHibernateJobStorage UseFluentNHibernateJobStorage(
            this IBootstrapperConfiguration configuration,
            string nameOrConnectionString, ProviderTypeEnum providerType, FluentNHibernateStorageOptions options = null)
        {
            var storage = FluentNHibernateStorageFactory.For(providerType, nameOrConnectionString, options);
            configuration.UseStorage(storage);


            return storage;
        }

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
            var storage = FluentNHibernateStorageFactory.For(providerType, nameOrConnectionString, options);
            configuration.UseStorage(storage);
            return storage;
        }
    }
}