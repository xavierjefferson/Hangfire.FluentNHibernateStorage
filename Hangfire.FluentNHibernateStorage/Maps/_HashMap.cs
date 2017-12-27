using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _HashMap : KeyValueTypeMapBase<_Hash, string>
    {
        private const string IndexName = "IX_Hash_Key_Field";

        public _HashMap()
        {
            Map(i => i.Field).Column("Field".WrapObjectName()).Length(40).Not.Nullable().UniqueKey(IndexName);
        }

        public override IndexTypeEnum KeyColumnIndexType => IndexTypeEnum.Unique;

        protected override string KeyColumnIndexName => IndexName;

        protected override string TableName => "Hangfire_Hash".WrapObjectName();

        protected override bool ValueNullable => true;

        protected override int? ValueLength => Constants.VarcharMaxLength;
    }
}