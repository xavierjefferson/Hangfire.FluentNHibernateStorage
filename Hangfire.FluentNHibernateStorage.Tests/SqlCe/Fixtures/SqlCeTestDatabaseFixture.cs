using System.Data.SqlServerCe;
using System.IO;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures
{
    public class SqlCeTestDatabaseFixture : DatabaseFixtureBase, IDatabaseFixture
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