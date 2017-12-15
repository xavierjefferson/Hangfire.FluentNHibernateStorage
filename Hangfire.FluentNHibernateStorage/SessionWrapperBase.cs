using System;
using Hangfire.Logging;

namespace Hangfire.FluentNHibernateStorage
{
    public abstract class SessionWrapperBase : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly object mutex = new object();
        private static int count;

        public SessionWrapperBase()
        {
            lock (mutex)
            {
                count++;

                Logger.DebugFormat("+Session {0} {1}", GetHashCode(), count);
            }
        }

        public virtual void Dispose()
        {
            lock (mutex)
            {
                count--;

                Logger.DebugFormat("-Session {0} {1}", GetHashCode(), count);
            }
        }
    }
}