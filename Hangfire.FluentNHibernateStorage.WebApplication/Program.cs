using Hangfire.FluentNHibernate.SampleStuff;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSerilog(
                    (hostBuilderContext, serviceProvider, loggerConfiguration) =>
                    {
                        loggerConfiguration
                            .ReadFrom.Configuration(hostBuilderContext.Configuration)
                            .Enrich.FromLogContext().Enrich.With(new ThreadIDEnricher()) 
                            .WriteTo.Debug(
                                outputTemplate:
                                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                            .WriteTo.SqliteSink(LogEventLevel.Information,
                                serviceProvider.GetService<ILogPersistenceService>())
                            .WriteTo
                            .SignalRSink<ChatHub, IChatHub>(
                                LogEventLevel.Information,
                                serviceProvider, sendAsString: false);
                    });
            ;
        }
    }
}