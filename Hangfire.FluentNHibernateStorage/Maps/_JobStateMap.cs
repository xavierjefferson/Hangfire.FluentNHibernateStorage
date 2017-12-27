using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobStateMap : Int64IdMapBase<_JobState>
    {
        public _JobStateMap()
        {
            Table("Hangfire_JobState".WrapObjectName());
            Map(i => i.Name).Column("Name".WrapObjectName()).Length(Constants.StateNameLength).Not.Nullable();
            Map(i => i.Reason).Column("Reason".WrapObjectName()).Length(Constants.StateReasonLength).Nullable();
            Map(i => i.Data).Column(Constants.ColumnNames.Data.WrapObjectName()).Length(Constants.StateDataLength).Nullable();
            Map(i => i.CreatedAt).Column(Constants.ColumnNames.CreatedAt.WrapObjectName()).Not.Nullable();
            References(i => i.Job).Column(Constants.ColumnNames.JobId.WrapObjectName()).Not.Nullable().Cascade.Delete();
        }
    }
}