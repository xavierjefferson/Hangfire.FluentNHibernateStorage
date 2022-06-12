using System;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Bogus;
using Hangfire.FluentNHibernateStorage;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernate.ConsoleApplication
{
    public class ThreadIDEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ThreadID", Thread.CurrentThread.Name));
        }
    }
    internal class Program
    {
        private static volatile int counter = 1;
        private static readonly Random r = new Random();
        private static readonly Faker f = new Faker();

        public static void WriteSomething(int currentCounter, TimeSpan interval)
        {
            Log.Logger.Information($"Background job #{currentCounter}, scheduled at by interval {interval}, {f.Rant.Review()}");
            Thread.Sleep(TimeSpan.FromSeconds(r.Next(5, 60)));
        }

        public static void HelloWorld(DateTime whenQueued, TimeSpan interval)
        {
            Log.Logger.Information(null, "Hello world at {2} intervals.  Enqueued={0}, Now={1}", whenQueued,
                DateTime.Now,
                interval);
            EnqueueAChain(interval);
        }

        private static void EnqueueAChain(TimeSpan interval)
        {
            string last = null;
            do
            {
                if (last != null)
                    last = BackgroundJob.ContinueJobWith(last, () => WriteSomething(counter, interval));
                else
                    last = BackgroundJob.Enqueue(() => WriteSomething(counter, interval));
            } while (r.NextDouble() > .95);

            counter++;
        }

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().Enrich.With(new ThreadIDEnricher())
                .WriteTo.Console(LogEventLevel.Debug, outputTemplate: "{Timestamp:HH:mm:ss.fff} ({ThreadID}) {Message:lj}{NewLine}{Exception}")
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
                    ProviderTypeEnum.SQLite,
                    new FluentNHibernateStorageOptions {QueuePollInterval = new TimeSpan(0, 0, 5)});

                using var server = new BackgroundJobServer();
                var values = new[] {1, 2, 3, 5, 7, 11, 13, 17, 23, 29};
                foreach (var item in values)
                {
                    RecurringJob.AddOrUpdate($"h{item}", () => HelloWorld(DateTime.Now, TimeSpan.FromMinutes(item)),
                        $"*/{item} * * * *");
                    Console.WriteLine($"Added job at {item} minute interval");
                }

                 
                Console.WriteLine("Hangfire Server started. Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't start server");
            }

            database.Delete();
        }
    }
}