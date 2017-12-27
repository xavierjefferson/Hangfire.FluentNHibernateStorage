using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobQueueMap : Int64IdMapBase<_JobQueue>
    {
        public _JobQueueMap()
        {
            Table("Hangfire_JobQueue".WrapObjectName());

            References(i => i.Job).Cascade.Delete().Column(Constants.ColumnNames.JobId.WrapObjectName());
            Map(i => i.FetchedAt).Column("FetchedAt".WrapObjectName()).Nullable();
            Map(i => i.Queue).Column("Queue".WrapObjectName()).Length(50).Not.Nullable();
            Map(i => i.FetchToken).Column("FetchToken".WrapObjectName()).Length(36).Nullable();
        }
    }
}