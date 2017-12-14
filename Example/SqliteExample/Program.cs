using System;
using System.Reflection;
using System.Timers;
using Hangfire;
using Hangfire.FluentNHibernateStorage;
using log4net;

namespace SqliteExample
{
    internal class Program
    {
        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private BackgroundJobServer _backgroundJobServer;
        private System.Timers.Timer t=null;

        public static void HelloWorld()
        {
            loggerNew.Info("Hello world at 2 min intervals");
        }
        public static void HelloWorld5()
        {
            loggerNew.Info("Hello world at 5 min intervals");
        }

        public static void Display(string x)
        {
            loggerNew.InfoFormat("Display {0}",x);
        }
        internal void Start()
        {
            t = new Timer(60000);
            t.Elapsed += (a, b) =>
            {
                BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString()));
            };
            _backgroundJobServer = new BackgroundJobServer();
            RecurringJob.AddOrUpdate(()=>HelloWorld(), Cron.MinuteInterval(2));
            RecurringJob.AddOrUpdate(() => HelloWorld5(), Cron.MinuteInterval(5));
        }

        internal void Stop()
        {
            if (t != null) { 
            t.Stop();
                t = null;
            }
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