using System;
using FluentNHibernate.Cfg.Db;
using NHibernate.Driver;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        private static T ConfigureProvider<T, U>(Func<PersistenceConfiguration<T, U>> createFunc,
            string connectionString, FluentNHibernateStorageOptions options) where T : PersistenceConfiguration<T, U>
            where U : ConnectionStringBuilder, new()
        {
            var provider = createFunc().ConnectionString(connectionString);
            
            if (!string.IsNullOrWhiteSpace(options.DefaultSchema))
            {
                provider.DefaultSchema(options.DefaultSchema);
            }
            return provider;
        }

        public static FluentNHibernateJobStorage For(ProviderTypeEnum providerType, string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            options = options ?? new FluentNHibernateStorageOptions();
            var configurer = GetPersistenceConfigurer(providerType, connectionString, options);

            return new FluentNHibernateJobStorage(configurer, options, providerType);
        }


        public static IPersistenceConfigurer GetPersistenceConfigurer(ProviderTypeEnum providerType,
            string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            options = options ?? new FluentNHibernateStorageOptions();

            IPersistenceConfigurer configurer;
            switch (providerType)
            {
                case ProviderTypeEnum.OracleClient10Managed:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle10, connectionString, options).Driver<OracleManagedDataClientDriver>();

                    break;
                case ProviderTypeEnum.OracleClient9Managed:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle9, connectionString, options).Driver<OracleManagedDataClientDriver>();
                    break;

                case ProviderTypeEnum.OracleClient10:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle10, connectionString, options);
                    
                    break;
                case ProviderTypeEnum.OracleClient9:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle9, connectionString, options);
                    break;
                case ProviderTypeEnum.PostgreSQLStandard:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.Standard, connectionString, options);

                    break;
                case ProviderTypeEnum.PostgreSQL81:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.PostgreSQL81, connectionString,
                        options);

                    break;
                case ProviderTypeEnum.PostgreSQL82:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.PostgreSQL82, connectionString,
                        options);

                    break;
                case ProviderTypeEnum.Firebird:
                    configurer = ConfigureProvider(() => new FirebirdConfiguration(), connectionString, options);

                    break;
 
                case ProviderTypeEnum.DB2Informix1150:
                    configurer = ConfigureProvider(() => DB2Configuration.Informix1150, connectionString, options);

                    break;
                case ProviderTypeEnum.DB2Standard:
                    configurer = ConfigureProvider(() => DB2Configuration.Standard, connectionString, options);

                    break;
                case ProviderTypeEnum.MySQL:
                    configurer = ConfigureProvider(() => MySQLConfiguration.Standard, connectionString, options);

                    break;
                case ProviderTypeEnum.MsSql2008:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2008, connectionString, options);

                    break;
                case ProviderTypeEnum.MsSql2012:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2012, connectionString, options);

                    break;
                case ProviderTypeEnum.MsSql2005:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2005, connectionString, options);

                    break;
                case ProviderTypeEnum.MsSql2000:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2000, connectionString, options);

                    break;
                default:
                    throw new ArgumentException("type");
            }
            return configurer;
        }
    }
}