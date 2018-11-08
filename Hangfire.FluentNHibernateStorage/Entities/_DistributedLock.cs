using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock : Int32IdBase
    {
        public _DistributedLock()
        {
            CreatedAt = DateTime.UtcNow;
            ExpireAtAsLong = DateTime.Now.ToUnixDate();
        }

        /// <summary>
        /// This is a long integer because NHibernate's default storage for dates
        /// doesn't have accuracy smaller than 1 second.
        /// </summary>
        public virtual long ExpireAtAsLong { get; set; }
        public virtual string Resource { get; set; }
        public virtual DateTime CreatedAt { get; set; }
    }
}