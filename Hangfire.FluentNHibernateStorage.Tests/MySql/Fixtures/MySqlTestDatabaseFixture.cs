using System;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using MySql.Data.MySqlClient;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures
{
    public class MySqlTestDatabaseFixture : DatabaseFixtureBase
    {
        private const string DatabaseVariable = "Hangfire_MySql_DatabaseName";

        private const string ConnectionStringTemplateVariable
            = "Hangfire_MySql_ConnectionStringTemplate";

        private const string MasterDatabaseName = "sys";
        private const string DefaultDatabaseName = @"Hangfire_MySql_Tests";

        private const string DefaultConnectionStringTemplate
            = @"Server=localhost;user=root;database={0}";

        private static readonly object GlobalLock = new object();

        public MySqlTestDatabaseFixture()
        {
            Monitor.Enter(GlobalLock);
            CreateDatabase();
        }

        public override ProviderTypeEnum ProviderType => ProviderTypeEnum.MySQL;


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

        public override string GetConnectionString()
        {
            return string.Format(GetConnectionStringTemplate(), GetDatabaseName());
        }

        private static string GetConnectionStringTemplate()
        {
            return Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable)
                   ?? DefaultConnectionStringTemplate;
        }


        public override void CreateDatabase()
        {
            var recreateDatabaseSql = string.Format(
                @"CREATE DATABASe IF NOT EXISTS {0}",
                GetDatabaseName());

            using (var connection = new MySqlConnection(GetMasterConnectionString()))
            {
                connection.Open();
                using (var sqlCommand = new MySqlCommand(recreateDatabaseSql, connection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}