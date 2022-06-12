using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobQueueMap : Int32IdMapBase<_JobQueue>
    {
        public _JobQueueMap()
        {
            References(i => i.Job).Cascade.Delete().Column(Constants.ColumnNames.JobId.WrapObjectName());
            Map(i => i.FetchedAt).Column("FetchedAt".WrapObjectName()).Nullable().CustomType<ForcedUtcDateTimeType>();
            Map(i => i.Queue).Column("Queue".WrapObjectName()).Length(50).Not.Nullable();
            Map(i => i.FetchToken).Column("FetchToken".WrapObjectName()).Length(36).Nullable();
        }

        public override string Tablename => "JobQueue";
    }
}