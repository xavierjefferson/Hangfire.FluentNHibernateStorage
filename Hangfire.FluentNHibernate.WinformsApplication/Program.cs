using System;
using System.Windows.Forms;
using Hangfire.FluentNHibernate.SampleStuff;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.WinformsApplication
{
    internal static class Program
    {
        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));
            services.AddScoped<Form1>();
            services.AddSingleton<ILogEventEmitterService, LogEventEmitterService>();
            services.AddSingleton<ISqliteTempFileService, SqliteTempFileService>();
            services.AddSingleton<IJobMethods, JobMethods>();
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var services = new ServiceCollection();

            ConfigureServices(services);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                Log.Logger = new LoggerConfiguration().Enrich.With(new ThreadIDEnricher()).Enrich
                    .With(new ThreadIDEnricher())
                    .WriteTo.Debug(
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                    .WriteTo.LogEventRepositorySink(serviceProvider.GetService<ILogEventEmitterService>())
                    .WriteTo.Console(LogEventLevel.Debug,
                        "{Timestamp:HH:mm:ss.fff} ({ThreadID}) {Message:lj}{NewLine}{Exception}")
                     
                    .CreateLogger();

                var form1 = serviceProvider.GetRequiredService<Form1>();
                Application.Run(form1);
            }
        }
    }
}