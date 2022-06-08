using System.Data.SqlServerCe;
using System.IO;
using System.Threading;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class SqlCeTestDatabaseFixture : TestDatabaseFixture, IDatabaseFixture
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


        public override void EnsurePersistenceConfigurer()
        {
            var databaseFileName = GetDatabaseFileName();
            var connectionString = $"Data Source={databaseFileName};";
            PersistenceConfigurer = FluentNHibernateStorageFactory.GetPersistenceConfigurer(
                ProviderTypeEnum.MsSqlCe40,
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
            var connectionString = $"Data Source={databaseFileName};";
            if (!File.Exists(databaseFileName))
            {
                var engine = new SqlCeEngine(connectionString);
                engine.CreateDatabase();
            }
        }


        private string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, "database.sdf");
        }
    }
}