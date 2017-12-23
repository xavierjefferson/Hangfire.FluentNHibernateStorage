using System;
using System.Data.SqlClient;
 
using System.IO;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class TestDatabaseFixture : IDisposable
    {
        private static readonly object GlobalLock = new object();

        public TestDatabaseFixture()
        {
            Monitor.Enter(GlobalLock);
            CreateAndInitializeDatabase();
        }

        public void Dispose()
        {
            DropDatabase();
            Monitor.Exit(GlobalLock);
        }

        private static void CreateAndInitializeDatabase()
        {
            using (var connection = new SqlConnection(ConnectionUtils.GetMasterConnectionString()))
            {
                connection.Open();
                using (var sc = new SqlCommand(
                   string.Format( "if not EXISTS (SELECT name FROM sys.databases WHERE name = '{0}') Create Database [{0}]", ConnectionUtils.GetDatabaseName()), connection))
                {
                    sc.ExecuteNonQuery();
                }
            }
        }

        private static void DropDatabase()
        {
            var fluentNHibernateJobStorage = ConnectionUtils.GetStorage();
            fluentNHibernateJobStorage.ResetAll();
        }
    }
}