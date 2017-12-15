using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public abstract class EntityBase1<T> : EntityBase0, IExpirableWithKey, IExpirableWithId
    {
        public virtual T Value { get; set; }
        public virtual string Key { get; set; }
        public virtual DateTime? ExpireAt { get; set; }
    }
}