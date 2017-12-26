# Hangfire FluentNHibernate Storage - An Implementation for MS SQL Server, MySQL, PostgreSQL, Oracle, Firebird, and DB/2
[![Latest version](https://img.shields.io/nuget/v/Hangfire.FluentNHibernateStorage.svg)](https://www.nuget.org/packages/Hangfire.FluentNHibernateStorage/) 

FluentNHibernate storage implementation of [Hangfire](http://hangfire.io/) - fire-and-forget, delayed and recurring tasks runner for .NET. Scalable and reliable background job runner. Supports multiple servers, CPU and I/O intensive, long-running and short-running jobs.

Forked from [Hangfire.MySqlStorage](https://github.com/arnoldasgudas/Hangfire.MySqlStorage), this is an NHibernate-backed implementation of a Hangfire storage provider that supports MS SQL Server, MySQL, PostgreSQL, Oracle, Firebird, and DB/2.  When deployed in a Hangfire instance, this library will automatically generate tables required for storing Hangfire metadata, and pass the correct SQL flavor to the database transparently.  The intention of doing an implementation like this one is to be able to share tentative improvements with a broad audience of developers.

FluentNHibernate / NHibernate enthusiasts may note that while NHibernate supports the SQLite, MS Access (Jet), and SQL Server Compact Edition desktop databases, none of these proved to work, and there's no plan to support them.

## Installation


Run the following command in the NuGet Package Manager console to install Hangfire.FluentNHibernateStorage:

```
Install-Package Hangfire.FluentNHibernateStorage
```

IMPORTANT!  For the use of any database other than SQL Server, you must also install an appropriate driver package:

### For MySQL:
```
Install-Package MySql.Data
```

### For PostgreSQL:
```
Install-Package Npgsql
```

### For Oracle Managed Provider):
```
Install-Package Oracle.ManagedDataAccess
```

### For Firebird:
```
Install-Package FirebirdSql.Data.FirebirdClient
```


## Database Implementation Notes
The package includes an enumeration of database providers AND their specific flavors of SQL across various SQL versions:
```
    public enum ProviderTypeEnum
    {
        None = 0,
      
        OracleClient10 = 3,
        OracleClient9 = 4,
        PostgreSQLStandard = 5,
        PostgreSQL81 = 6,
        PostgreSQL82 = 7,
        Firebird = 8,
       
        DB2Informix1150 = 10,
        DB2Standard = 11,
        MySQL = 12,
        MsSql2008 = 13,
        MsSql2012 = 14,
        MsSql2005 = 15,
        MsSql2000 = 16,
        OracleClient10Managed = 17,
        OracleClient9Managed = 18,
    }
```
The enumeration values correspond to the list of providers NHibernate supports.  When you instantiate a provider, you'll pass the best enumeration value to the FluentNHibernateStorageFactory.For method, along with your connection string.  I wrote it this way so you don't have to be concerned with the underlying implementation details for the various providers, which can be a little messy.  

###MS Sql Server
You'll note that four versions of SQL Server are included.  If you're using MS SQL Server 2000 (and I hope you're not), you may have a rough time because the database does not support strings longer than 8000 characters.  This implementation was tested against MS Sql Server 2012 and 2008.

###Oracle
Be advised that two of the four Oracle options (OracleClient9Managed, OracleClient10Managed) use the **Oracle.ManagedDataAccess** client library internally, while the other two (OracleClient9, OracleClient10) use the **System.Data.OracleClient** client library.  I'm not Oracle savvy, and I could only get **Oracle.ManagedDataAccess** to work properly (the other is NHibernate's default).  You may have a different experience.  This implementation was tested against Oracle 11g Express on Oracle Linux.

###PostgreSQL
This implementation was tested against PostgreSQL 10 on Ubuntu 12.

###DB/2
This implementation was tested against IBM® Db2® Express-C on Windows.

###MySQL
This implementation was tested against MySQL 5.7.20 on Ubuntu 16.

###Firebird
I set out to test this implementation on all the RDBMS systems NHibernate supports, and this was the last on the list.  I could not get a database instance to work.  Your mileage may vary :)

###Usage - Within an ASP.NET Application
I may simplify the implementation later, but I think this code is pretty painless  Usage within an ASP.Net application would probably employ the OWin startup approach for Hangfire, which is pretty well-documented.  Please note the properties, which include specifying a database schema, passed to the method:
```
        public void Configuration(IAppBuilder app)
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
				DefaultSchema = null // use database provider's default schema
            };           
            var storage = FluentNHibernateStorageFactory.For(ProviderTypeEnum.MySQL, "MyConnectionStringHere", options);
            GlobalConfiguration.Configuration
                .UseStorage(storage);
            /*** More Hangfire configuration stuff 
            would go in this same method ***/
         }
```
###Usage - A Standalone Server
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
				DefaultSchema = null // use database provider's default schema
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
- `DefaultSchema` - database schema into which the Hangfire tables will be created.  Default is database provider specific ("dbo" for SQL Server, "public" for PostgreSQL, etc).

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

#Database Stuff

 - **IMPORTANT**:  The Hangfire engine, with its 20 default worker threads, is not database-intensive but it can be VERY chatty.
 - During first-time use, you'll need table-creation rights on your RDBMS.
 - You can't specify table names (yet).  But you can specify the schema.  See the sample code.
 - Since this uses an OR/M, there are no stored procedures or views to be installed.
 - All dates stored in the tables are UTC, for consistency; except for the Firebird implementation because Firebird lacks a "current UTC date" function.
 - This implementation uses database transactions to cleanly distribute pending jobs between various workers threads.  You may witness, if you turn on a logger, lots of failed transactions.  For now, this is expected.  Don't panic.
 - Old records are automatically cleaned up.  Most of the cleanup parameters are specified by the Hangfire engine itself, and this implementation does its best to not lock up your RDDMS while old records are being purged.  For more information on specific cleanup parameters, consult the Hangfire forums.
 - Table Hangfire_Dual should contain one and only one row - this is by design, and it's required in order to support the various RDBMSs.

##Tables Created:
```
Hangfire_AggregatedCounter
Hangfire_Counter
Hangfire_DistributedLock
Hangfire_Dual
Hangfire_Hash
Hangfire_Job
Hangfire_JobParameter
Hangfire_JobQueue
Hangfire_JobState
Hangfire_List
Hangfire_Server
Hangfire_Set
```