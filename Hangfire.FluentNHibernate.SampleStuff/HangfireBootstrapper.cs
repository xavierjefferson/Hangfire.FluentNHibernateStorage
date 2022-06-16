using System;
using Hangfire.FluentNHibernateStorage;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public static class HangfireBootstrapper
    {
        public static IGlobalConfiguration SetupJobStorage(this IGlobalConfiguration globalConfiguration,
            ISqliteTempFileService sqliteTempFileService)
        {
            return globalConfiguration.UseFluentNHibernateJobStorage(
                sqliteTempFileService.GetConnectionString(),
                ProviderTypeEnum.SQLite);
        }

        public static IGlobalConfiguration SetupActivator(
            this IGlobalConfiguration globalConfiguration, IServiceProvider serviceProvider)
        {
            return globalConfiguration.UseActivator(new HangfireActivator(serviceProvider));
        }
    }
}