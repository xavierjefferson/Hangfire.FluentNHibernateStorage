using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures
{
    public class SqlServerTestDatabaseFixture : DatabaseFixtureBase, IDatabaseFixture
    {
        private const string DatabaseVariable = "Hangfire_SqlServer_DatabaseName";

        private const string ConnectionStringTemplateVariable
            = "Hangfire_SqlServer_ConnectionStringTemplate";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName = @"Hangfire.SqlServer.Tests";

        private const string DefaultConnectionStringTemplate
            = @"Server=.;Database={0};Trusted_Connection=True;";

        private static readonly object GlobalLock = new object();


        public SqlServerTestDatabaseFixture()
        {
            Monitor.Enter(GlobalLock);
            CreateDatabase();
        }


        public override void EnsurePersistenceConfigurer()
        {
            var connectionString = GetConnectionString();
            PersistenceConfigurer = FluentNHibernateStorageFactory.GetPersistenceConfigurer(
                ProviderTypeEnum.MsSql2012,
                connectionString);
        }

        public override void Cleanup()
        {
            try

            {
                //var recreateDatabaseSql = string.Format(
                //    @"if not db_id('{0}') is null drop database [{0}]",
                //    GetDatabaseName());

                //using (var connection = new SqlConnection(GetMasterConnectionString()))
                //{
                //    connection.Open();
                //    using (var sqlCommand = new SqlCommand(recreateDatabaseSql, connection))
                //    {
                //        sqlCommand.ExecuteNonQuery();
                //    }
                //}
            }
            catch
            {
            }
        }

        public override void OnDispose()
        {
            Monitor.Exit(GlobalLock);
            Cleanup();
        }

        public static string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable(DatabaseVariable) ?? DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return string.Format(GetConnectionStringTemplate(), MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return string.Format(GetConnectionStringTemplate(), GetDatabaseName());
        }

        private static string GetConnectionStringTemplate()
        {
            return Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable)
                   ?? DefaultConnectionStringTemplate;
        }

        public static DbConnection CreateConnection(bool microsoftDataSqlClient)
        {
            var connection =
                new SqlConnection(GetConnectionString());

            connection.Open();

            return connection;
        }

        public override void CreateDatabase()
        {
            var recreateDatabaseSql = string.Format(
                @"if db_id('{0}') is null create database [{0}] COLLATE SQL_Latin1_General_CP1_CS_AS",
                GetDatabaseName());

            using (var connection = new SqlConnection(GetMasterConnectionString()))
            {
                connection.Open();
                using (var sqlCommand = new SqlCommand(recreateDatabaseSql, connection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}