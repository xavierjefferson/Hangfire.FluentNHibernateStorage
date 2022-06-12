using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _DistributedLockMap : Int32IdMapBase<_DistributedLock>
    {
        public _DistributedLockMap()
        {
            Map(i => i.Resource).Column("Resource".WrapObjectName()).Length(100).Not.Nullable().Unique();
            this.MapCreatedAt();
            Map(i => i.ExpireAtAsLong).Column("ExpireAtAsLong".WrapObjectName()).Not.Nullable();
        }

        public override string Tablename => "DistributedLock";
    }
}