using Hangfire.FluentNHibernateStorage.Entities;
using NHibernate.Linq.Functions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _SetMap : EntityBase1Map<_Set, string>
    {
        public _SetMap()
        {
            Map(i => i.Score).Column("`Score`").Not.Nullable();
        }

        protected override bool HasUniqueKey
        {
            get { return true; }
        }

        protected override string KeyObjectName
        {
            get { return "IX_Set_Key_Value"; }
        }

        protected override string TableName
        {
            get { return "`Hangfire_Set`"; }
        }

        protected override bool ValueNullable
        {
            get { return false; }
        }
        protected override bool ValueInKey{get {
            return true;
        }}
        protected override int? ValueLength
        {
            get { return 255; }
        }
    }
}