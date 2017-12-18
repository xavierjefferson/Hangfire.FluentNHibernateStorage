using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _AggregatedCounterMap : EntityBase1Map<_AggregatedCounter, long>
    {
        protected override bool HasUniqueKey => true;

        protected override string KeyObjectName => "IX_CounterAggregated_Key";

        protected override string TableName => "Hangfire_AggregatedCounter".WrapObjectName();

        protected override bool ValueNullable => false;

        protected override int? ValueLength => null;
    }
}