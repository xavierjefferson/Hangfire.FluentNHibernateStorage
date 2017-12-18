using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _HashMap : EntityBase1Map<_Hash, string>
    {
        private const string UniqueIndexName = "IX_Hash_Key_Field";

        public _HashMap()
        {
            Map(i => i.Field).Column("Field".WrapObjectName()).Length(40).Not.Nullable().UniqueKey(UniqueIndexName);
        }

        protected override bool HasUniqueKey => true;

        protected override string KeyObjectName => UniqueIndexName;

        protected override string TableName => "Hangfire_Hash".WrapObjectName();

        protected override bool ValueNullable => true;

        protected override int? ValueLength => Constants.VarcharMaxLength;
    }
}