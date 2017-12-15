using System;
using Hangfire.Logging;

namespace Hangfire.FluentNHibernateStorage
{
    public abstract class SessionWrapperBase : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly object mutex = new object();
        private static int _sessionCount;

        protected SessionWrapperBase()
        {
            lock (mutex)
            {
                _sessionCount++;

                Logger.DebugFormat("+Session {0} {1}", GetHashCode(), _sessionCount);
            }
        }

        public virtual void Dispose()
        {
            lock (mutex)
            {
                _sessionCount--;

                Logger.DebugFormat("-Session {0} {1}", GetHashCode(), _sessionCount);
            }
        }
    }
}