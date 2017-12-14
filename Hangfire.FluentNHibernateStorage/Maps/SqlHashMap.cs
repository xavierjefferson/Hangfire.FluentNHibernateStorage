using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class SqlHashMap : Gen1Map<_Hash, string>
    {
        public SqlHashMap()
        {
            Map(i => i.Field).Column("`Field`").Length(40).Not.Nullable().UniqueKey("IX_Hash_Key_Field");
        }

        protected override bool UniqueKey
        {
            get { return true; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_HASH_Key_Field"; }
        }

        protected override string TableName
        {
            get { return "`Hash`"; }
        }

        protected override bool ValueNullable
        {
            get { return true; }
        }

        protected override int? ValueLength
        {
            get { return int.MaxValue; }
        }
    }
}