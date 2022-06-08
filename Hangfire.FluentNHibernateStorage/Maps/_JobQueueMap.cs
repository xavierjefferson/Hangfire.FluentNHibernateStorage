using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobQueueMap : Int32IdMapBase<_JobQueue>
    {
        public _JobQueueMap()
        {
            Table("JobQueue");

            References(i => i.Job).Cascade.Delete().Column(Constants.ColumnNames.JobId.WrapObjectName());
            this.Map(i => ((IFetchedAtNullable) i).FetchedAt).Column("FetchedAt".WrapObjectName()).Nullable().CustomType<ForcedUtcDateTimeType>();
            Map(i => i.Queue).Column("Queue".WrapObjectName()).Length(50).Not.Nullable();
            Map(i => i.FetchToken).Column("FetchToken".WrapObjectName()).Length(36).Nullable();
        }
    }
}