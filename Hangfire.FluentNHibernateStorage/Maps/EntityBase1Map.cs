using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal abstract class EntityBase1Map<T, U> : IntIdMap<T> where T : EntityBase1<U>
    {
        protected EntityBase1Map()
        {
            Table(TableName);
            LazyLoad();

            var keyPropertyPart = Map(i => i.Key).Column("Key".WrapObjectName()).Not.Nullable();
            if (HasUniqueKey)
            {
                keyPropertyPart.UniqueKey(KeyObjectName);
            }
            else
            {
                keyPropertyPart.Index(KeyObjectName);
            }

            var valuePropertyPart = Map(i => i.Value).Column("Value".WrapObjectName());
            if (ValueInKey)
            {
                valuePropertyPart.UniqueKey(KeyObjectName);
            }
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
            Map(i => i.ExpireAt).Column("ExpireAt".WrapObjectName()).Nullable();
        }

        protected virtual bool ValueInKey => false;

        protected abstract bool HasUniqueKey { get; }
        protected abstract string KeyObjectName { get; }
        protected abstract string TableName { get; }
        protected abstract bool ValueNullable { get; }
        protected abstract int? ValueLength { get; }
    }
}