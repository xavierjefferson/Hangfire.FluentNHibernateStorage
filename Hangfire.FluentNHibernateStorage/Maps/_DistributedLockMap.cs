using Hangfire.FluentNHibernateStorage.Entities;
using NHibernate.Type;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : Int32IdMapBase<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Table("DistributedLock");
            Map(i => i.Resource).Column("Resource".WrapObjectName()).Length(100).Not.Nullable().Unique();
            this.MapCreatedAt();
            Map(i => i.ExpireAtAsLong).Column("`ExpireAtAsLong`").Not.Nullable();
        }
    }
}