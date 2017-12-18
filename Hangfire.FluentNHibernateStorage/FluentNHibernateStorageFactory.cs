using System;
using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        public static FluentNHibernateStorage For(PersistenceConfigurerEnum type, string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            IPersistenceConfigurer configurer;
            switch (type)
            {
                case PersistenceConfigurerEnum.MsSqlCeStandard:
                    configurer = MsSqlCeConfiguration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MsSqlCe40:
                    configurer = MsSqlCeConfiguration.MsSqlCe40.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.JetDriver:
                    configurer = JetDriverConfiguration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.OracleClient10:
                    configurer = OracleClientConfiguration.Oracle10.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.OracleClient9:
                    configurer = OracleClientConfiguration.Oracle9.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.PostgreSQLStandard:
                    configurer = PostgreSQLConfiguration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.PostgreSQL81:
                    configurer = PostgreSQLConfiguration.PostgreSQL81.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.PostgreSQL82:
                    configurer = PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.Firebird:
                    configurer = new FirebirdConfiguration().ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.SQLite:
                    configurer = SQLiteConfiguration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.Db2Informix1150:
                    configurer = DB2Configuration.Informix1150.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.Db2Standard:
                    configurer = DB2Configuration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MySql:
                    configurer = MySQLConfiguration.Standard.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MsSql2008:
                    configurer = MsSqlConfiguration.MsSql2008.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MsSql2012:
                    configurer = MsSqlConfiguration.MsSql2012.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MsSql2005:
                    configurer = MsSqlConfiguration.MsSql2005.ConnectionString(connectionString);
                    break;
                case PersistenceConfigurerEnum.MsSql2000:
                    configurer = MsSqlConfiguration.MsSql2000.ConnectionString(connectionString);
                    break;
                default:
                    throw new ArgumentException("type");
            }
            return new FluentNHibernateStorage(configurer, options);
        }
    }
}