using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _DistributedLock:IIntId
    {
        
        public virtual string Resource { get; set; }
        public virtual DateTime CreatedAt { get; set; }
        public virtual int Id { get; set; }
    }
}