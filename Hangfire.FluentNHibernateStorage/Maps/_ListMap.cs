using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ListMap : EntityBase1Map<_List, string>
    {
        public _ListMap()
        {
            Table("Hangfire_List".WrapObjectName());
        }

        protected override int? ValueLength => Constants.VarcharMaxLength;

        protected override bool HasUniqueKey => false;

        protected override string KeyObjectName => "IX_LIST_KEY";

        protected override string TableName => "List".WrapObjectName();

        protected override bool ValueNullable => true;
    }
}