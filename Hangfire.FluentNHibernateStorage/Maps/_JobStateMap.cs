using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobStateMap : IntIdMap<_JobState>
    {
        public _JobStateMap()
        {
            Table("`Hangfire_JobState`");

            Map(i => i.Name).Column("`Name`").Length(20).Not.Nullable();
            Map(i => i.Reason).Column("`Reason`").Length(100).Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAtColumnName).Not.Nullable();
            Map(i => i.Data).Column(Constants.DataColumnName).Length(Constants.VarcharMaxLength).Nullable();
            References(i => i.Job).Column(Constants.JobIdColumnName).Not.Nullable().Cascade.Delete();
        }
    }
}