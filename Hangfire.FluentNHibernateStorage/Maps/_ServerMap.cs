using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ServerMap : ClassMap<_Server>
    {
        public _ServerMap()
        {
            Table("Server");
            Id(i => i.Id).Length(100).GeneratedBy.Assigned().Column(Constants.ColumnNames.Id.WrapObjectName());
            Map(i => i.Data).Length(Constants.VarcharMaxLength).Not.Nullable()
                .Column(Constants.ColumnNames.Data.WrapObjectName());
            Map(i => i.LastHeartbeat).Nullable().Column("LastHeartbeat".WrapObjectName());
        }
    }
}