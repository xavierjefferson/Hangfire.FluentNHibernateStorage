using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    /// <summary>
    ///     base mapping class for Counter, AggregatedCounter, Set, Hash, List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    internal abstract class KeyValueTypeMapBase<T, U> : Int32IdMapBase<T> where T : KeyValueTypeBase<U>
    {
        protected KeyValueTypeMapBase()
        {
            Table(TableName.WrapObjectName());
            LazyLoad();

            var keyPropertyPart = Map(i => i.Key).Column("Key".WrapObjectName()).Not.Nullable();
            switch (KeyColumnIndexType)
            {
                case IndexTypeEnum.Unique:
                    keyPropertyPart.UniqueKey(KeyColumnIndexName);
                    break;
                case IndexTypeEnum.Nonunique:
                    keyPropertyPart.Index(KeyColumnIndexName);
                    break;
            }

            var valuePropertyPart = Map(i => i.Value).Column("Value".WrapObjectName());
          

            if (ValueNullable)
                valuePropertyPart.Nullable();
            else
                valuePropertyPart.Not.Nullable();

            if (ValueLength.HasValue) valuePropertyPart.Length(ValueLength.Value);

            this.MapExpireAt();
        }

        public abstract IndexTypeEnum KeyColumnIndexType { get; }

       

        protected abstract string KeyColumnIndexName { get; }
        protected abstract string TableName { get; }
        protected abstract bool ValueNullable { get; }
        protected abstract int? ValueLength { get; }
    }
}