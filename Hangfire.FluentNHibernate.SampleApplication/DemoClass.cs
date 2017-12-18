using System;
using System.Configuration;
using System.Transactions;
using Hangfire.FluentNHibernateStorage;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public class DemoClass
    {
        private static BackgroundJobServer _backgroundJobServer;

        private static void Main(string[] args)
        {
            //Configure properties (this is optional)
            var options = new FluentNHibernateStorageOptions
            {
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
            };

            //THIS SECTION GETS THE STORAGE PROVIDER
            var PersistenceConfigurerType = PersistenceConfigurerEnum.MsSql2012;
            var connectionString = ConfigurationManager.ConnectionStrings["someConnectionString"].ConnectionString;
            var storage = FluentNHibernateStorageFactory.For(PersistenceConfigurerType, connectionString, options);

            //THIS LINE CONFIGURES HANGFIRE WITH THE STORAGE PROVIDER
            GlobalConfiguration.Configuration.UseStorage(storage);
            /*THIS LINE STARTS THE BACKGROUND SERVER*/
            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                storage.GetBackgroundProcesses());
            /*AND... DONE.*/
            Console.WriteLine("Background job server is running.  Press [ENTER] to quit.");
            Console.ReadLine();
        }
    }
}