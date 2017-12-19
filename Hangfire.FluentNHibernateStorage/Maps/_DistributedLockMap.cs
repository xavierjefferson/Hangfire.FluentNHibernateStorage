using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : ClassMap<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Table("Hangfire_DistributedLock".WrapObjectName());
            Id(i => i.Id).Column(Constants.Id).Length(40).GeneratedBy.Assigned();
            Map(i => i.Resource).Column("Resource".WrapObjectName()).Length(100).Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAt).Not.Nullable();
        }
    }
}