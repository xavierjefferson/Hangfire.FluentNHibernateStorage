using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _CounterMap : KeyValueTypeMapBase<_Counter, long>
    {
        public override IndexTypeEnum KeyColumnIndexType => IndexTypeEnum.Nonunique;

        protected override string KeyColumnIndexName => "IX_Counter_Key";

        protected override string TableName => "Hangfire_Counter".WrapObjectName();

        protected override bool ValueNullable => false;

        protected override int? ValueLength => null;
    }
}