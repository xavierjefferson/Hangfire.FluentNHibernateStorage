using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobQueueMap : IntIdMap<_JobQueue>
    {
        public _JobQueueMap()
        {
            Table("`Hangfire_JobQueue`");

            References(i => i.Job).Cascade.All().Column(Constants.JobId);
            Map(i => i.FetchedAt).Column("`FetchedAt`").Nullable();
            Map(i => i.Queue).Column("`Queue`").Length(50).Not.Nullable();
            Map(i => i.FetchToken).Column("`FetchToken`").Length(36).Nullable();
        }
    }
}