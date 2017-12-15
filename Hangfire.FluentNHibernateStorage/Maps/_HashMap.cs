using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _HashMap : EntityBase1Map<_Hash, string>
    {
        private const string UniqueIndexName = "IX_Hash_Key_Field";

        public _HashMap()
        {
            Map(i => i.Field).Column("`Field`").Length(40).Not.Nullable().UniqueKey(UniqueIndexName);
        }

        protected override bool HasUniqueKey
        {
            get { return true; }
        }

        protected override string KeyObjectName
        {
            get { return UniqueIndexName; }
        }

        protected override string TableName
        {
            get { return "`Hangfire_Hash`"; }
        }

        protected override bool ValueNullable
        {
            get { return true; }
        }

        protected override int? ValueLength
        {
            get { return Constants.VarcharMaxLength; }
        }
    }
}