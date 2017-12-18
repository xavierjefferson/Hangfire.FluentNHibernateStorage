using System;
using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        private static IPersistenceConfigurer ConfigureProvider<T, U>(Func<PersistenceConfiguration<T, U>> createFunc,
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

        public static FluentNHibernateJobStorage For(PersistenceConfigurerEnum type, string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            options = options ?? new FluentNHibernateStorageOptions();

            IPersistenceConfigurer configurer;
            switch (type)
            {
                case PersistenceConfigurerEnum.MsSqlCeStandard:

                    configurer = ConfigureProvider(() => MsSqlCeConfiguration.Standard, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MsSqlCe40:
                    configurer = ConfigureProvider(() => MsSqlCeConfiguration.MsSqlCe40, connectionString, options);
                    break;
                case PersistenceConfigurerEnum.JetDriver:
                    configurer = ConfigureProvider(() => JetDriverConfiguration.Standard, connectionString, options);
                    break;
                case PersistenceConfigurerEnum.OracleClient10:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle10, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.OracleClient9:
                    configurer = ConfigureProvider(() => OracleClientConfiguration.Oracle9, connectionString, options);
                    break;
                case PersistenceConfigurerEnum.PostgreSQLStandard:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.Standard, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.PostgreSQL81:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.PostgreSQL81, connectionString,
                        options);

                    break;
                case PersistenceConfigurerEnum.PostgreSQL82:
                    configurer = ConfigureProvider(() => PostgreSQLConfiguration.PostgreSQL82, connectionString,
                        options);

                    break;
                case PersistenceConfigurerEnum.Firebird:
                    configurer = ConfigureProvider(() => new FirebirdConfiguration(), connectionString, options);

                    break;
                case PersistenceConfigurerEnum.SQLite:
                    configurer = ConfigureProvider(() => SQLiteConfiguration.Standard, connectionString, options);
               
                    break;
                case PersistenceConfigurerEnum.Db2Informix1150:
                    configurer = ConfigureProvider(() => DB2Configuration.Informix1150, connectionString, options);
              
                    break;
                case PersistenceConfigurerEnum.Db2Standard:
                    configurer = ConfigureProvider(() => DB2Configuration.Standard, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MySql:
                    configurer = ConfigureProvider(() => MySQLConfiguration.Standard, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MsSql2008:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2008, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MsSql2012:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2012, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MsSql2005:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2005, connectionString, options);

                    break;
                case PersistenceConfigurerEnum.MsSql2000:
                    configurer = ConfigureProvider(() => MsSqlConfiguration.MsSql2000, connectionString, options);

                    break;
                default:
                    throw new ArgumentException("type");
            }

            return new FluentNHibernateJobStorage(configurer, options);
        }
    }
}