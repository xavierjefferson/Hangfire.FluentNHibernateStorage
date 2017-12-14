using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class SqlJobMap : ClassMap<_Job>
    {
        public SqlJobMap()
        {
            Table("Job");
            LazyLoad();
            Id(i => i.Id).GeneratedBy.Identity().Not.Nullable().Column("`Id`");
            Map(i => i.StateName).Column("`StateName`").Length(20).Nullable().Index("IX_Job_StateName");
            Map(i => i.InvocationData).Column("`InvocationData`").Length(int.MaxValue).Not.Nullable();
            Map(i => i.Arguments).Column("`Arguments`").Length(int.MaxValue).Not.Nullable();
            Map(i => i.CreatedAt).Column("`CreatedAt`").Not.Nullable();
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
            References(i => i.Parameters).Column("`JobId`").Cascade.All();
            References(i => i.SqlStates).Column("`JobId`").Cascade.All();
        }
    }
}