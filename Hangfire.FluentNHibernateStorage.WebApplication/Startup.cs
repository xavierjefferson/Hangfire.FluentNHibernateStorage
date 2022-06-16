using System;
using Hangfire.FluentNHibernate.SampleStuff;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<SqliteTempFileService>();
            var sqliteTempFileService = new SqliteTempFileService(logger);

            services.AddHangfire(x =>
                x.SetupJobStorage(sqliteTempFileService));
            services.AddHangfireServer();
            services.AddRazorPages();
            services.AddSingleton<IJobMethods, JobMethods>();
            services.AddSingleton<ISqliteTempFileService>(sqliteTempFileService);
            services.AddSignalR() ;
            var sessionFactory = SessionFactoryBuilder.GetFromAssemblyOf<LogItemMap>(ProviderTypeEnum.SQLite,
                sqliteTempFileService.GetConnectionString()).SessionFactory;
            services.AddSingleton(sessionFactory);
            services.AddSingleton<ILogPersistenceService, LogPersistenceService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            GlobalConfiguration.Configuration.SetupActivator(serviceProvider);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseHangfireDashboard("/mydashboard");
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(e => { e.MapHub<ChatHub>("/chatHub"); });


            app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });

            JobMethods.CreateRecurringJobs(serviceProvider.GetService<ILogger<Startup>>());
        }
    }
}