using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class JobParameterMap : ClassMap<_JobParameter>
    {
        public JobParameterMap()
        {
            Id(i => i.Id).Column("`Id`").GeneratedBy.Identity();
            References(i => i.Job).Column("`JobId`").Not.Nullable().Cascade.All();
            Map(i => i.Name).Column("`Name`").Length(40).Not.Nullable();
            Map(i => i.Value).Column("`Value`").Length(int.MaxValue).Nullable();
        }
    }
}