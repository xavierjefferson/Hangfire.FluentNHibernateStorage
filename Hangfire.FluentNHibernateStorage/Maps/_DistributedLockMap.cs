using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : ClassMap<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Table("`DistributedLock`");
            Id(i => i.DistributedLockId).Column("`DistributedLockId`").GeneratedBy.Identity();
            Map(i => i.Resource).Column("`Resource`").Length(100).Not.Nullable();
            Map(i => i.CreatedAt).Column("`CreatedAt`").Not.Nullable();
        }
    }
}