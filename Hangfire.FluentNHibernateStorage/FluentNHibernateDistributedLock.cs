using System;
using System.Threading;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateDistributedLock : FluentNHibernateDistributedLockBase, IDisposable, IComparable
    {
        public FluentNHibernateDistributedLock(FluentNHibernateStorage storage, string resource, TimeSpan timeout,
            CancellationToken? cancellationToken = null) : base(storage, resource, timeout, cancellationToken)
        {
            Session = storage.GetStatefulSession();
        }
    }
}