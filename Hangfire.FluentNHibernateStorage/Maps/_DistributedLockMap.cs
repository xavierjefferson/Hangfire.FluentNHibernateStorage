using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : IntIdMap<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Table("Hangfire_DistributedLock".WrapObjectName());
            Map(i => i.Resource).Column("Resource".WrapObjectName()).Length(100).Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAt).Not.Nullable();
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
        }
    }
}