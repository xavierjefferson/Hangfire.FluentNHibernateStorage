using System.Data.SQLite;
using System.IO;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures
{
    public class SqliteTestDatabaseFixture : DatabaseFixtureBase
    {
        private static readonly object GlobalLock = new object();
        private static DirectoryInfo _testFolder;

        public SqliteTestDatabaseFixture()
        {
            _testFolder = new DirectoryInfo(GetTempPath());
            _testFolder.Create();
            Monitor.Enter(GlobalLock);
            CreateDatabase();
        }

        public override ProviderTypeEnum ProviderType => ProviderTypeEnum.SQLite;

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

        public override string GetConnectionString()
        {
            var databaseFileName = GetDatabaseFileName();
            return $"Data Source={databaseFileName};Version=3";
        }

        public override void OnDispose()
        {
            Monitor.Exit(GlobalLock);
            Cleanup();
        }

        public override void CreateDatabase()
        {
            var databaseFileName = GetDatabaseFileName();
            if (!File.Exists(databaseFileName)) SQLiteConnection.CreateFile(databaseFileName);
        }


        private string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, "database.sqlite");
        }
    }
}