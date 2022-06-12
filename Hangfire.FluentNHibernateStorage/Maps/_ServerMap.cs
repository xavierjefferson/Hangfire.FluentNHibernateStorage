using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ServerMap : ClassMap<_Server>
    {
        public _ServerMap()
        {
            Table("Server".WrapObjectName());
            Id(i => i.Id).Length(200).GeneratedBy.Assigned().Column(Constants.ColumnNames.Id.WrapObjectName());
            Map(i => i.Data).Length(Constants.VarcharMaxLength).Not.Nullable()
                .Column(Constants.ColumnNames.Data.WrapObjectName());
            Map(i => i.LastHeartbeat).Nullable().Column("LastHeartbeat".WrapObjectName())
                .CustomType<ForcedUtcDateTimeType>();
        }
    }
}