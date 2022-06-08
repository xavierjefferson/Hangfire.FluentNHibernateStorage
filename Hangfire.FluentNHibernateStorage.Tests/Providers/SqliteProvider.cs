using System.Data.SQLite;
using System.IO;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.Providers
{
    public class SqliteProvider : ProviderBase, IDbProvider
    {
        private static DirectoryInfo _testFolder;


        public SqliteProvider()
        {
            _testFolder = new DirectoryInfo(GetTempPath());
            _testFolder.Create();
        }


        public override void EnsurePersistenceConfigurer()
        {
            var databaseFileName = GetDatabaseFileName();
            var connectionString = $"Data Source={databaseFileName};Version=3;";
            if (!File.Exists(databaseFileName))
                PersistenceConfigurer = FluentNHibernateStorageFactory.GetPersistenceConfigurer(
                    ProviderTypeEnum.SQLite,
                    connectionString);
        }

        public void Cleanup()
        {
            try
            {
                File.Delete(GetDatabaseFileName());
            }
            catch
            {
            }
        }

        public override void CreateDatabase()
        {
            var databaseFileName = GetDatabaseFileName();
            var connectionString = $"Data Source={databaseFileName};Version=3;";
            if (!File.Exists(databaseFileName)) SQLiteConnection.CreateFile(databaseFileName);
        }

        public override void DestroyDatabase()
        {
            Cleanup();
        }


        private string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, Instance + "database.sqlite");
        }
    }
}