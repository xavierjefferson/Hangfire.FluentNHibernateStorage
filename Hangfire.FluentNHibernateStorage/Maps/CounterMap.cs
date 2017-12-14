using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class CounterMap : Gen1Map<_Counter, long>
    {
        protected override bool UniqueKey
        {
            get { return false; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_Counter_Key"; }
        }

        protected override string TableName
        {
            get { return "`Counter`"; }
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