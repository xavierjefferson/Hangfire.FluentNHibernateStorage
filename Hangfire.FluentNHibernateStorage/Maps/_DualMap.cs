using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DualMap : ClassMap<_Dual>
    {
        public _DualMap()
        {
            Table("Hangfire_Dual".WrapObjectName());
            Id(i => i.Id).Column(Constants.Id).GeneratedBy.Assigned();
        }
    }
}