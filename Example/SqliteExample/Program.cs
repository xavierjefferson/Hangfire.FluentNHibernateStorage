using System;
using System.Configuration;
using System.Reflection;
using System.Timers;
using log4net;

namespace Hangfire.FluentNHibernateStorage.SqlServerExample
{
    internal class Program
    {
        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static FluentNHibernateStorage storage;
        private BackgroundJobServer _backgroundJobServer;
        private Timer t;

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
            loggerNew.InfoFormat("Display {0}", x);
        }

        internal void Start()
        {
            t = new Timer(60000);
            t.Elapsed += (a, b) => { BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString())); };

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                storage.GetBackgroundProcesses());

            RecurringJob.AddOrUpdate(() => HelloWorld(), Cron.MinuteInterval(2));
            RecurringJob.AddOrUpdate(() => HelloWorld5(), Cron.MinuteInterval(5));
        }

        internal void Stop()
        {
            if (t != null)
            {
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
            
            storage = FluentNHibernateStorageFactory.ForMsSql2012("Server=.\\sqlexpress;Database=hftest;Trusted_Connection=True;");
            GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                .UseStorage(storage);

            loggerNew.InfoFormat("{1} instance on {0}", Environment.MachineName, typeof(Program).FullName);
            var instance = new Program();
            instance.Start();
            Console.ReadLine();
        }
    }
}