using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock
    {
        public virtual int DistributedLockId { get; set; }
        public virtual string Resource { get; set; }
        public virtual DateTime CreatedAt { get; set; }
    }
}