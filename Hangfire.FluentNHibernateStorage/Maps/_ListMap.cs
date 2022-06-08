using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ListMap : KeyValueTypeMapBase<_List, string>
    {
      

        public override IndexTypeEnum KeyColumnIndexType => IndexTypeEnum.Nonunique;

        protected override int? ValueLength => Constants.VarcharMaxLength;

        protected override string KeyColumnIndexName => "IX_LIST_KEY";

        protected override string TableName => "List";

        protected override bool ValueNullable => true;
    }
}