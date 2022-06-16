using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public class SqliteTempFileService : IDisposable, ISqliteTempFileService
    {
        private static DirectoryInfo _testFolder;


        public SqliteTempFileService(ILogger<SqliteTempFileService> logger)
        {
            _testFolder = new DirectoryInfo(GetTempPath());
            _testFolder.Create();

            CreateDatabase();
        }


        protected static string Instance => Guid.NewGuid().ToString();


        public void Dispose()
        {
            try

            {
                DeleteFolder(_testFolder);
            }
            catch
            {
            }
        }


        public string GetConnectionString()
        {
            var databaseFileName = GetDatabaseFileName();
            return $"Data Source={databaseFileName};Version=3";
        }

        public void CreateDatabase()
        {
            var databaseFileName = GetDatabaseFileName();
            if (!File.Exists(databaseFileName))
            {
                Log.Information((Exception) null, "Using file {0}", databaseFileName);
                SQLiteConnection.CreateFile(databaseFileName);
            }
            else
            {
                Log.Information((Exception) null, "File {0} exists", databaseFileName);
            }
        }

        public string GetDatabaseFileName()
        {
            return Path.Combine(_testFolder.FullName, "database.sqlite");
        }


        protected void DeleteFolder(DirectoryInfo directoryInfo)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                }

            foreach (var info in directoryInfo.GetDirectories())
                try
                {
                    DeleteFolder(info);
                }
                catch
                {
                }

            directoryInfo.Delete();
        }


        protected string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, Instance);
        }
    }
}