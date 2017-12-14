using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : ClassMap<_Job>
    {
        public _JobMap()
        {
            Table("Job");
            LazyLoad();
            Id(i => i.Id).GeneratedBy.Identity().Not.Nullable().Column("`Id`");
            
            Map(i => i.InvocationData).Column("`InvocationData`").Length(int.MaxValue).Not.Nullable();
            Map(i => i.Arguments).Column("`Arguments`").Length(int.MaxValue).Not.Nullable();
            Map(i => i.CreatedAt).Column("`CreatedAt`").Not.Nullable();
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
            HasMany(i => i.Parameters).KeyColumn("`JobId`").Cascade.All();
            HasMany(i => i.History).KeyColumn("`JobId`").Cascade.All();
            References(i => i.CurrentState).Column("`StateId`").Cascade.All().Nullable();
        }
    }
}