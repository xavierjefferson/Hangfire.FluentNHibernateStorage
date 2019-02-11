using System;
using System.Configuration;
using System.Data.SqlClient;
using FluentNHibernate.Cfg.Db;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public static class ConnectionUtils
    {
        private static readonly object Mutex = new object();
        private static IPersistenceConfigurer _configurer;
        private static string _conn;
        private static string _conn2;

        public static string GetConnectionString()
        {
            lock (Mutex)
            {
                if (_conn == null)
                {
                    _conn = ConfigurationManager.ConnectionStrings["main"].ConnectionString;
                    Console.WriteLine("Using db conn {0}", _conn);
                }

                return _conn;
            }
        }

        public static string GetDatabaseName()
        {
            return new SqlConnectionStringBuilder(GetConnectionString()).InitialCatalog;
        }

        public static string GetMasterConnectionString()
        {
            lock (Mutex)
            {
                if (_conn2 == null)
                {
                    _conn2 =
                        new SqlConnectionStringBuilder(GetConnectionString()) {InitialCatalog = "master"}.ToString();
                    Console.WriteLine("Using db conn master {0}", _conn2);
                }

                return _conn2;
            }
        }

        public static FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            return new FluentNHibernateJobStorage(GetPersistenceConfigurer());
        }

        public static IPersistenceConfigurer GetPersistenceConfigurer()
        {
            lock (Mutex)
            {
                return _configurer ?? (_configurer = FluentNHibernateStorageFactory.GetPersistenceConfigurer(
                           ProviderTypeEnum.MsSql2008,
                           GetConnectionString()));
            }
        }
    }
}