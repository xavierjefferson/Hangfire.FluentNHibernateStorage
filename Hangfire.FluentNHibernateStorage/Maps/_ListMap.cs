using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ListMap : EntityBase1Map<_List, string>
    {
        public _ListMap()
        {
            Table("`Hangfire_List`");
        }

        protected override int? ValueLength
        {
            get { return Constants.VarcharMaxLength; }
        }

        protected override bool HasUniqueKey
        {
            get { return false; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_LIST_KEY"; }
        }

        protected override string TableName
        {
            get { return "`List`"; }
        }

        protected override bool ValueNullable
        {
            get { return true; }
        }
    }
}