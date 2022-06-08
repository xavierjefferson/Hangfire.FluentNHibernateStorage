using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal static class MapHelpers
    {
        public static void MapCreatedAt<T>(this ClassMap<T> item) where T : ICreatedAt
        {
            item.Map(i => i.CreatedAt).Column(Constants.ColumnNames.CreatedAt.WrapObjectName()).Not.Nullable()
                .CustomType<ForcedUtcDateTimeType>();
        }

        public static void MapExpireAt<T>(this ClassMap<T> item) where T : IExpireAtNullable
        {
            item.Map(i => i.ExpireAt).Column("ExpireAt".WrapObjectName()).Nullable().CustomType<ForcedUtcDateTimeType>();
        }
    }
}