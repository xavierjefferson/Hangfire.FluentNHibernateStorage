using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public abstract class CounterBase : IExpirableWithId, IInt32Id, IStringKey, IIntValue
    {
        public virtual DateTime? ExpireAt { get; set; }
        public virtual int Id { get; set; }
        public virtual int Value { get; set; }
        public virtual string Key { get; set; }
    }
}