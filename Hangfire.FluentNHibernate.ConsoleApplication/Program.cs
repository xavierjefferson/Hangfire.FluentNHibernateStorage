using System;
using System.Data.SQLite;
using System.IO;
using Hangfire.FluentNHibernateStorage;
using Serilog;
using Serilog.Events;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernate.ConsoleApplication
{
    internal class Program
    {
        public static void HelloWorld(DateTime whenQueued, TimeSpan interval)
        {
            Log.Logger.Information(null, "Hello world at {2} intervals.  Enqueued={0}, Now={1}", whenQueued,
                DateTime.Now,
                interval);
        }

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Debug)
                .CreateLogger();

            var database = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sqlite"));
            Log.Information((Exception) null, "Using file {0}", database.FullName);
            if (!database.Exists)
            {
                Log.Information((Exception) null, "File {0} doesn't exist, creating", database.FullName);
                SQLiteConnection.CreateFile(database.FullName);
            }
            else
            {
                Log.Information((Exception) null, "File {0} exists", database.FullName);
            }

            Console.WriteLine("Hello World!");
            Log.Information((Exception) null, "Starting server");

            try
            {
                GlobalConfiguration.Configuration.UseFluentNHibernateJobStorage(
                    string.Format("Data Source={0};Version=3;", database.FullName),
                    ProviderTypeEnum.SQLite, new FluentNHibernateStorageOptions(){QueuePollInterval = new TimeSpan(0,0,5)});

                using (var server = new BackgroundJobServer())
                {
                    RecurringJob.AddOrUpdate("h2",() => HelloWorld(DateTime.Now, TimeSpan.FromMinutes(2)), "*/2 * * * *");
                    RecurringJob.AddOrUpdate("h5",() => HelloWorld(DateTime.Now, TimeSpan.FromMinutes(5)), "*/5 * * * *");
                    RecurringJob.AddOrUpdate("h1",() => HelloWorld(DateTime.Now, TimeSpan.FromMinutes(1)), "* * * * *");
                    RecurringJob.AddOrUpdate("h7",() => HelloWorld(DateTime.Now, TimeSpan.FromMinutes(7)), "*/7 * * * *");
                    RecurringJob.AddOrUpdate("h7", () => HelloWorld(DateTime.Now, TimeSpan.FromSeconds(30)), "*/30 * * * * *");
                    Console.WriteLine("Hangfire Server started. Press any key to exit...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't start server");
            }
            database.Delete();
        }
    }
}