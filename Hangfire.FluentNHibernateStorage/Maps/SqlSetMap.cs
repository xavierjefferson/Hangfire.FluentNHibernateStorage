using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class SqlSetMap : ClassMap<_Set>
    {
        public SqlSetMap()
        {
            Table("`Set`");
            Id(i => i.Id).Column("`Id`").GeneratedBy.Identity();
            Map(i => i.Key).Column("`Key`").Length(100).Not.Nullable();
            Map(i => i.Value).Column("`Value`").Length(256).Not.Nullable();
            Map(i => i.Score).Column("`Score`").Not.Nullable();
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
        }
    }
}