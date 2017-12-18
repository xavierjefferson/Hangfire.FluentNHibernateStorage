using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    public class _DistributedLockMap : ClassMap<_DistributedLock>
    {
        public const string TableName = "Hangfire_DistributedLock";
        public const string CreatedAtColumnName = "CreatedAt";
        public const string ResourceColumnName = "Resource";

        public _DistributedLockMap()
        {
            Table($"`{TableName}`");
            LazyLoad();
            Id(i => i.Id).Column(Constants.IdColumnName).GeneratedBy.Assigned().Length(40);
            Map(i => i.Resource).Column($"`{ResourceColumnName}`").Length(100).Not.Nullable().UniqueKey("aa");
            Map(i => i.CreatedAt).Column($"`{CreatedAtColumnName}`").Not.Nullable().UniqueKey("aa");
        }
    }
}