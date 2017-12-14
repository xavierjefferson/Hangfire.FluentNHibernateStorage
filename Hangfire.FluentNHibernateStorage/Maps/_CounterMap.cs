using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _CounterMap : EntityBase1Map<_Counter, long>
    {
        protected override bool HasUniqueKey
        {
            get { return false; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_Counter_Key"; }
        }

        protected override string TableName
        {
            get { return "`Hangfire_Counter`"; }
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