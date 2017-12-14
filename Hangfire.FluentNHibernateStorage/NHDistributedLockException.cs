using System;

namespace Hangfire.FluentNHibernateStorage
{
    public class NHDistributedLockException : Exception
    {
        public NHDistributedLockException(string message) : base(message)
        {
        }
    }
}