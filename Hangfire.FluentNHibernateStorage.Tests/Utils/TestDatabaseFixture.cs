using System;
using System.Threading;
using Dapper;
using MySql.Data.MySqlClient;

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
            var recreateDatabaseSql = string.Format(
                @"CREATE DATABASE IF NOT EXISTS `{0}`",
                ConnectionUtils.GetDatabaseName());

            using (var connection = new MySqlConnection(
                ConnectionUtils.GetMasterConnectionString()))
            {
                connection.Execute(recreateDatabaseSql);
            }

            
        }

        private static void DropDatabase()
        {
            var recreateDatabaseSql = string.Format(
                @"DROP DATABASE IF EXISTS `{0}`",
                ConnectionUtils.GetDatabaseName());

            using (var connection = new MySqlConnection(
                ConnectionUtils.GetMasterConnectionString()))
            {
                connection.Execute(recreateDatabaseSql);
            }
        }
    }
}