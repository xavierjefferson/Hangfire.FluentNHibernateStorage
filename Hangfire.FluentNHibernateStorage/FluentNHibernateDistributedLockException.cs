using System;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLockTimeoutException : Exception
    {
        public FluentNHibernateDistributedLockTimeoutException(string message) : base(message)
        {
        }
    }
}