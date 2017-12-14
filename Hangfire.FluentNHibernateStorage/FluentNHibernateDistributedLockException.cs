using System;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLockException : Exception
    {
        public FluentNHibernateDistributedLockException(string message) : base(message)
        {
        }
    }
}