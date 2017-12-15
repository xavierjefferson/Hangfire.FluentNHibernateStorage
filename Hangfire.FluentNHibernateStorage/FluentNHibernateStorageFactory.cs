using FluentNHibernate.Cfg.Db;

namespace Hangfire.FluentNHibernateStorage
{
    public sealed class FluentNHibernateStorageFactory
    {
        public static FluentNHibernateStorage ForMsCe(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlCeConfiguration.Standard.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForMsCe40(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlCeConfiguration.MsSqlCe40.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForJetDriver(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(JetDriverConfiguration.Standard.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForOracle10(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(OracleClientConfiguration.Oracle10.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForOracle9(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(OracleClientConfiguration.Oracle9.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForPostgreSQL(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.Standard.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForPostgreSQL81(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.PostgreSQL81.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForPostgreSQL82(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForFirebird(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(new FirebirdConfiguration().ConnectionString(connectionString), options);
        }

        public static FluentNHibernateStorage ForSQLiteWithFile(string fileName, string password = null,
            FluentNHibernateStorageOptions options = null)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                return new FluentNHibernateStorage(
                    SQLiteConfiguration.Standard.UsingFileWithPassword(fileName, password),
                    options);
            }
            return new FluentNHibernateStorage(SQLiteConfiguration.Standard.UsingFile(fileName),
                options);
        }

        public static FluentNHibernateStorage ForSQLite(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(SQLiteConfiguration.Standard.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForDB2Informix1150(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(DB2Configuration.Informix1150.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForDB2Standard(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(DB2Configuration.Standard.ConnectionString(connectionString), options);
        }

        public static FluentNHibernateStorage ForMySQL(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MySQLConfiguration.Standard.ConnectionString(connectionString), options);
        }

        public static FluentNHibernateStorage ForMsSql2008(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForMsSql2000(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2000.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForMsSql2005(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2005.ConnectionString(connectionString),
                options);
        }

        public static FluentNHibernateStorage ForMsSql2012(string connectionString,
            FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateStorage(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString),
                options);
        }
    }
}