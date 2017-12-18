using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _CounterMap : EntityBase1Map<_Counter, long>
    {
        protected override bool HasUniqueKey => false;

        protected override string KeyObjectName => "IX_Counter_Key";

        protected override string TableName => "Hangfire_Counter".WrapObjectName();

        protected override bool ValueNullable => false;

        protected override int? ValueLength => null;
    }
}