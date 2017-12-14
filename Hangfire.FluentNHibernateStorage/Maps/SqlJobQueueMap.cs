using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class SqlJobQueueMap : ClassMap<_JobQueue>
    {
        public SqlJobQueueMap()
        {
            Table("JobQueue");
            Id(i => i.Id).Column("`Id`").GeneratedBy.Assigned();
            References(i => i.Job).Cascade.All().Column("`JobId`");
            Map(i => i.FetchedAt).Column("`FetchedAt`").Nullable();
            Map(i => i.Queue).Column("`Queue`").Length(50).Not.Nullable();
            Map(i => i.FetchToken).Column("`FetchToken`").Length(36).Nullable();
        }
    }
}