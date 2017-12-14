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
            Map(i => i.CreatedAt).Column(Constants.CreatedAt).Not.Nullable();
            Map(i => i.Data).Column(Constants.Data).Length(int.MaxValue).Nullable();
            References(i => i.Job).Column(Constants.JobId).Not.Nullable().Cascade.All();
        }
    }
}