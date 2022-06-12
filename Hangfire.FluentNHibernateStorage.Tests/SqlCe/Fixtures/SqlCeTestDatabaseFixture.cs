using System.Data.SqlServerCe;
using System.IO;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures
{
    public class SqlCeTestDatabaseFixture : DatabaseFixtureBase
    {
        private static readonly object GlobalLock = new object();

        private static DirectoryInfo _testFolder;

        public SqlCeTestDatabaseFixture()
        {
            _testFolder = new DirectoryInfo(GetTempPath());
            _testFolder.Create();
            Monitor.Enter(GlobalLock);
            CreateDatabase();
        }

        public override ProviderTypeEnum ProviderType => ProviderTypeEnum.MsSqlCe40;


        public override void Cleanup()
        {
            try

            {
                DeleteFolder(_testFolder);
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

        public override string GetConnectionString()
        {
            var databaseFileName = GetDatabaseFileName();
            return $"Data Source={databaseFileName};";
        }

        public override void CreateDatabase()
        {
            var databaseFileName = GetDatabaseFileName();

            if (!File.Exists(databaseFileName))
            {
                var engine = new SqlCeEngine(GetConnectionString());
                engine.CreateDatabase();
            }
        }


        private string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, "database.sdf");
        }
    }
}