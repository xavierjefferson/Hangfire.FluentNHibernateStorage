using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock:EntityBase0, IExpirableWithId
    {
        public _DistributedLock()
        {
            CreatedAt = 0;
            ExpireAt = DateTime.UtcNow.AddDays(7);
        }

        public virtual string Resource { get; set; }
        public virtual long CreatedAt { get; set; }
        public virtual DateTime? ExpireAt { get; set; }
    }
}