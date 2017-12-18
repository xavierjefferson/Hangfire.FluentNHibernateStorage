using System;
using System.Threading;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStatelessDistributedLock : FluentNHibernateDistributedLockBase, IDisposable,
        IComparable
    {
        public FluentNHibernateStatelessDistributedLock(FluentNHibernateJobStorage storage, string resource,
            TimeSpan timeout,
            CancellationToken? cancellationToken = null) : base(storage, resource, timeout, cancellationToken)
        {
            Session = storage.GetStatelessSession();
        }
    }
}