using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class EntityBase1Map<T, U> : ClassMap<T> where T : EntityBase1<U>
    {
        protected EntityBase1Map()
        {
            Table(TableName);
            LazyLoad();
            Id(i => i.Id).Column("`Id`").GeneratedBy.Identity();
            var keyPropertyPart = Map(i => i.Key).Column("`Key`").Not.Nullable();
            if (HasUniqueKey)
            {
                keyPropertyPart.UniqueKey(KeyObjectName);
            }
            else
            {
                keyPropertyPart.Index(KeyObjectName);
            }
            var valuePropertyPart = Map(i => i.Value).Column("`Value`");
            if (ValueNullable)
            {
                valuePropertyPart.Nullable();
            }
            else
            {
                valuePropertyPart.Not.Nullable();
            }
            if (ValueLength.HasValue)
            {
                valuePropertyPart.Length(ValueLength.Value);
            }
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
        }

        protected abstract bool HasUniqueKey { get; }
        protected abstract string KeyObjectName { get; }
        protected abstract string TableName { get; }
        protected abstract bool ValueNullable { get; }
        protected abstract int? ValueLength { get; }
    }
}