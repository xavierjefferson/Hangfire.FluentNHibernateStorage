using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock : EntityBase0
    {
        public _DistributedLock()
        {
            CreatedAt = DateTime.UtcNow;
            ExpireAtAsLong = DateTime.Now.ToUnixDate();
        }

        public virtual long ExpireAtAsLong { get; set; }
        public virtual string Resource { get; set; }
        public virtual DateTime CreatedAt { get; set; }
    }
}