using System.Data.SQLite;
using System.IO;
using System.Threading;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class SqliteTestDatabaseFixture : TestDatabaseFixture, IDatabaseFixture
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


        public override void EnsurePersistenceConfigurer()
        {
            var databaseFileName = GetDatabaseFileName();
            var connectionString = $"Data Source={databaseFileName};Version=3";
            PersistenceConfigurer = FluentNHibernateStorageFactory.GetPersistenceConfigurer(
                ProviderTypeEnum.SQLite,
                connectionString);
        }

        public void Cleanup()
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

        public override IDatabaseFixture GetProvider()
        {
            return this;
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