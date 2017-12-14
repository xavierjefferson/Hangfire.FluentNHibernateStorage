using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _AggregatedCounterMap : EntityBase1Map<_AggregatedCounter, long>
    {
        protected override bool HasUniqueKey
        {
            get { return true; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_CounterAggregated_Key"; }
        }

        protected override string TableName
        {
            get { return "`AggregatedCounter`"; }
        }

        protected override bool ValueNullable
        {
            get { return false; }
        }

        protected override int? ValueLength
        {
            get { return null; }
        }
    }
}