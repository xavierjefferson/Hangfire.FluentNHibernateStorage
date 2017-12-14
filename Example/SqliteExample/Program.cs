using System;
using System.Reflection;
using Hangfire;
using Hangfire.FluentNHibernateStorage;
using log4net;

namespace SqliteExample
{
    internal class Program
    {
        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private BackgroundJobServer _backgroundJobServer;

 

        internal void Start()
        {
            _backgroundJobServer = new BackgroundJobServer();
        }

        internal void Stop()
        {
            if (_backgroundJobServer != null)
            {
                _backgroundJobServer.SendStop();
                _backgroundJobServer.Dispose();
            }
            _backgroundJobServer = null;
        }

        private static void Main(string[] args)
        {
          
            var filename = System.IO.Path.Combine(System.Environment.CurrentDirectory, "test.sqlite");
            GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                .UseStorage(FluentNHibernateStorageFactory.ForSQLiteWithFile(filename));
            loggerNew.InfoFormat("{1} instance on {0}", Environment.MachineName, typeof(Program).FullName);
            var instance = new Program();
            instance.Start();
            Console.ReadLine();
        }
    }
}