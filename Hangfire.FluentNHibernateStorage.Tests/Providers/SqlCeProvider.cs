using System;
using System.Data.SqlServerCe;
using System.IO;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.Tests.Providers
{
    public class SqlCeProvider : ProviderBase, IDbProvider
    {
        private static DirectoryInfo _testFolder;

        public SqlCeProvider()
        {
            _testFolder = new DirectoryInfo(GetTempPath());
            _testFolder.Create();
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
                var databaseFileName = GetDatabaseFileName();
                if (File.Exists(databaseFileName))
                    File.Delete(databaseFileName);
            }
            catch (Exception ex)
            {
            }
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

        public override void DestroyDatabase()
        {
            Cleanup();
        }


        private string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, "database.sdf");
        }
    }
}