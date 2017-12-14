using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class Gen1Map<T, X> : ClassMap<T> where T : EntityBase1<X>
    {
        protected Gen1Map()
        {
            Table(TableName);
            LazyLoad();
            Id(i => i.Id).Column("`Id`").GeneratedBy.Identity();
            var a = Map(i => i.Key).Column("`Key`").Not.Nullable();
            if (UniqueKey)
            {
                a.UniqueKey(KeyObjectName);
            }
            else
            {
                a.Index(KeyObjectName);
            }
            var v = Map(i => i.Value).Column("`Value`");
            if (ValueNullable)
            {
                v.Nullable();
            }
            else
            {
                v.Not.Nullable();
            }
            if (ValueLength.HasValue)
            {
                v.Length(ValueLength.Value);
            }
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
        }

        protected abstract bool UniqueKey { get; }
        protected abstract string KeyObjectName { get; }
        protected abstract string TableName { get; }
        protected abstract bool ValueNullable { get; }
        protected abstract int? ValueLength { get; }
    }
}