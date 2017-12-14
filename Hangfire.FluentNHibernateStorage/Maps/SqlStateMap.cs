using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class SqlStateMap : ClassMap<_JobState>
    {
        public SqlStateMap()
        {
            Table("JobState");
            Id(i => i.Id).Column("`Id`").GeneratedBy.Identity();
            Map(i => i.Name).Column("`Name`").Length(20).Not.Nullable();
            Map(i => i.Reason).Column("`Reason`").Length(100).Nullable();
            Map(i => i.CreatedAt).Column("`CreatedAt`").Not.Nullable();
            Map(i => i.Data).Column("`Data`").Length(int.MaxValue).Nullable();
            References(i => i.Job).Column("`JobId`").Not.Nullable().Cascade.All();
        }
    }
}