# Hangfire FluentNHibernate Storage Implementation
[![Latest version](https://img.shields.io/nuget/v/Hangfire.FluentNHibernateStorage.svg)](https://www.nuget.org/packages/Hangfire.FluentNHibernateStorage/) 

FluentNHibernate storage implementation of [Hangfire](http://hangfire.io/) - fire-and-forget, delayed and recurring tasks runner for .NET. Scalable and reliable background job runner. Supports multiple servers, CPU and I/O intensive, long-running and short-running jobs.

This implementation supports Hangfire storage with intended primary support on MS SQL Server, MySQL, PostgreSQL, Oracle, and DB2.  Firebird is also an option.  Though FluentNHibernate supports SQLite, MS Access (Jet), and SQL Server Compact Edition, none of these proved to work, and there's no plan to support them.

## Installation
 

Run the following command in the NuGet Package Manager console to install Hangfire.FluentNHibernateStorage:

```
Install-Package Hangfire.FluentNHibernateStorage
```

## Usage
```
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
            var PersistenceConfigurerType = ProviderTypeEnum.MsSql2012;
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
```
Description of optional parameters:
- `TransactionIsolationLevel` - transaction isolation level. Default is read committed.
- `QueuePollInterval` - job queue polling interval. Default is 15 seconds.
- `JobExpirationCheckInterval` - job expiration check interval (manages expired records). Default is 1 hour.
- `CountersAggregateInterval` - interval to aggregate counter. Default is 5 minutes.
- `PrepareSchemaIfNecessary` - if set to `true`, it creates database tables. Default is `true`.
- `DashboardJobListLimit` - dashboard job list limit. Default is 50000.
- `TransactionTimeout` - transaction timeout. Default is 1 minute.

### How to limit number of open connections

Number of opened connections depends on Hangfire worker count. You can limit worker count by setting `WorkerCount` property value in `BackgroundJobServerOptions`:
```
app.UseHangfireServer(
   new BackgroundJobServerOptions
   {
      WorkerCount = 1
   });
```
More info: http://hangfire.io/features.html#concurrency-level-control

## Dashboard
Hangfire provides a dashboard
![Dashboard](https://camo.githubusercontent.com/f263ab4060a09e4375cc4197fb5bfe2afcacfc20/687474703a2f2f68616e67666972652e696f2f696d672f75692f64617368626f6172642d736d2e706e67)
More info: [Hangfire Overview](http://hangfire.io/overview.html#integrated-monitoring-ui)

## Build
Please use Visual Studio or any other tool of your choice to build the solution

## Test
In order to run unit tests and integrational tests set the following variables in you system environment variables (restart of Visual Studio is required):

`Hangfire_SqlServer_ConnectionStringTemplate` (default: `server=127.0.0.1;uid=root;pwd=root;database={0};Allow User Variables=True`)

`Hangfire_SqlServer_DatabaseName` (default: `Hangfire.FluentNHibernate.Tests`)


