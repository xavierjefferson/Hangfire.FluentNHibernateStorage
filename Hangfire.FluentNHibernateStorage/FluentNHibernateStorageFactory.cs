using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        public static FluentNHibernateStorage ForOracle10(string connectionString)
        {
            return new FluentNHibernateStorage(OracleClientConfiguration.Oracle10.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForOracle9(string connectionString)
        {
            return new FluentNHibernateStorage(OracleClientConfiguration.Oracle9.ConnectionString(connectionString));
        }
      
        public static FluentNHibernateStorage ForPostgreSQL(string connectionString)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.Standard.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForPostgreSQL81(string connectionString)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.PostgreSQL81.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForPostgreSQL82(string connectionString)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForFirebird(string connectionString)
        {
            return new FluentNHibernateStorage(new FirebirdConfiguration());
        }

        public static FluentNHibernateStorage ForSQLite(string connectionString)
        {
            return new FluentNHibernateStorage(SQLiteConfiguration.Standard.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForDB2Informix1150(string connectionString)
        {
            return new FluentNHibernateStorage(DB2Configuration.Informix1150.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForDB2Standard(string connectionString)
        {
            return new FluentNHibernateStorage(DB2Configuration.Standard.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForMySQL(string connectionString)
        {
            return new FluentNHibernateStorage(MySQLConfiguration.Standard.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForMsSql2008(string connectionString)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForMsSql2000(string connectionString)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2000.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForMsSql2005(string connectionString)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2005.ConnectionString(connectionString));
        }
        public static FluentNHibernateStorage ForMsSql2012(string connectionString)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString));
        }
    }
}