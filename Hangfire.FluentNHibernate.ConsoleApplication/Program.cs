using System;
using Hangfire.FluentNHibernate.SampleStuff;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.ConsoleApplication
{
    internal class Program
    {
        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));
            services.AddSingleton<IJobMethods, JobMethods>();
        }

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().Enrich.With(new ThreadIDEnricher())
                .WriteTo.Console(LogEventLevel.Debug,
                    "{Timestamp:HH:mm:ss.fff} ({ThreadID}) {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Setting up dependency injection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();


            var logger = serviceProvider.GetService<ILogger<Program>>();
            var sqliteTempFileService =
                new SqliteTempFileService(serviceProvider.GetService<ILogger<SqliteTempFileService>>());


            Console.WriteLine("Hello World!");
            logger.LogInformation(null, "Starting server");

            try
            {
                var globalConfiguration = GlobalConfiguration.Configuration;
                globalConfiguration.SetupJobStorage(sqliteTempFileService)
                    .SetupActivator(serviceProvider);
                using var server = new BackgroundJobServer();
                JobMethods.CreateRecurringJobs(logger);


                Console.WriteLine("Hangfire Server started. Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Couldn't start server");
            }

            sqliteTempFileService.Dispose();
        }
    }
}