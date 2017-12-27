using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _SetMap : KeyValueTypeMapBase<_Set, string>
    {
        public _SetMap()
        {
            Map(i => i.Score).Column("Score".WrapObjectName()).Not.Nullable();
        }

        protected override string KeyColumnIndexName => "IX_Set_Key_Value";
        public override IndexTypeEnum KeyColumnIndexType => IndexTypeEnum.Unique;
        protected override string TableName => "Hangfire_Set".WrapObjectName();

        protected override bool ValueNullable => false;

        protected override bool ValueInKey => true;

        protected override int? ValueLength => 255;
    }
}