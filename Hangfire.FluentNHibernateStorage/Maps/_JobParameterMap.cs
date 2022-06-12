using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobParameterMap : Int32IdMapBase<_JobParameter>
    {
        public _JobParameterMap()
        {
            string indexName = $"IX_Hangfire_{Tablename}_JobIdAndName";

            References(i => i.Job).Column(Constants.ColumnNames.JobId.WrapObjectName()).Not.Nullable().Cascade.Delete()
                .UniqueKey(indexName).ForeignKey($"FK_Hangfire_{Tablename}_Job");
            Map(i => i.Name).Column("Name".WrapObjectName()).Length(40).Not.Nullable().UniqueKey(indexName);
            Map(i => i.Value).Column(Constants.ColumnNames.Value.WrapObjectName()).Length(Constants.VarcharMaxLength)
                .Nullable();
        }

        public override string Tablename => "JobParameter";
    }
}