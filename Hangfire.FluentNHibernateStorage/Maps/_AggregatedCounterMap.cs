using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _AggregatedCounterMap : KeyValueTypeMapBase<_AggregatedCounter, int>
    {
        public override IndexTypeEnum KeyColumnIndexType => IndexTypeEnum.Unique;

        protected override string KeyColumnIndexName => "IX_CounterAggregated_Key";

        protected override string TableName => "AggregatedCounter";

        protected override bool ValueNullable => false;

        protected override int? ValueLength => null;
    }
}