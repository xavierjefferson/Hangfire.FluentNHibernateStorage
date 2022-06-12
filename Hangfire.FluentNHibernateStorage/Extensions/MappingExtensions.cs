using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Maps;

namespace Hangfire.FluentNHibernateStorage.Extensions
{
    internal static class MappingExtensions
    {
        public static string WrapObjectName(this string x)
        {
            return string.Format("`{0}`", x);
        }

        public static IdentityPart MapIntIdColumn<T>(this ClassMap<T> item) where T : IInt32Id
        {
            return item.Id(i => i.Id).Column(Constants.ColumnNames.Id.WrapObjectName()).GeneratedBy.Identity();
        }

        public static PropertyPart MapCreatedAt<T>(this ClassMap<T> item) where T : ICreatedAt
        {
            return item.Map(i => i.CreatedAt).Column(Constants.ColumnNames.CreatedAt.WrapObjectName()).Not.Nullable()
                .CustomType<ForcedUtcDateTimeType>();
        }

        public static PropertyPart MapExpireAt<T>(this ClassMapBase<T> item) where T : IExpirable
        {
            return item.Map(i => i.ExpireAt).Column("ExpireAt".WrapObjectName()).Nullable()
                .CustomType<ForcedUtcDateTimeType>()
                .Index($"IX_Hangfire_{item.Tablename}_{nameof(IExpirable.ExpireAt)}");
        }

        public static PropertyPart MapStringKeyColumn<T>(this ClassMapBase<T> item) where T : IStringKey
        {
            return item.Map(i => i.Key).Column("Key".WrapObjectName()).Not.Nullable()
                .Index($"IX_Hangfire_{item.Tablename}_Key");
        }

        public static PropertyPart MapStringValueColumn<T>(this ClassMap<T> item, bool nullable)
            where T : IStringValue
        {
            var propertyPart = item.Map(i => i.Value).Column(Constants.ColumnNames.Value.WrapObjectName());
            return nullable ? propertyPart.Nullable() : propertyPart.Not.Nullable();
        }

        public static PropertyPart MapIntValueColumn<T>(this ClassMap<T> item) where T : IIntValue
        {
            var propertyPart = item.Map(i => i.Value).Column(Constants.ColumnNames.Value.WrapObjectName()).Not
                .Nullable();
            return propertyPart;
        }
    }
}