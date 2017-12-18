using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _SetMap : EntityBase1Map<_Set, string>
    {
        public _SetMap()
        {
            Map(i => i.Score).Column("Score".WrapObjectName()).Not.Nullable();
        }

        protected override bool HasUniqueKey => true;

        protected override string KeyObjectName => "IX_Set_Key_Value";

        protected override string TableName => "Hangfire_Set".WrapObjectName();

        protected override bool ValueNullable => false;

        protected override bool ValueInKey => true;

        protected override int? ValueLength => 255;
    }
}