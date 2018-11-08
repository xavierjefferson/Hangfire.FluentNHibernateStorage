using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : Int32IdMapBase<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Table("Hangfire_DistributedLock".WrapObjectName());
            Map(i => i.Resource).Column("Resource".WrapObjectName()).Length(100).Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.ColumnNames.CreatedAt.WrapObjectName()).Not.Nullable();
            Map(i => i.ExpireAtAsLong).Column("`ExpireAtAsLong`").Not.Nullable();
        }
    }
}