using FluentNHibernate.Mapping;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class ServerMap : ClassMap<Entities._Server>
    {
        public ServerMap()
        {
            Table("`Server`");
            Id(i => i.Id).Length(100).GeneratedBy.Assigned().Column("`Id`");
            Map(i => i.Data).Length(int.MaxValue).Not.Nullable().Column("`Data`");
            Map(i => i.LastHeartbeat).Not.Nullable().Column("`LastHeartBeat`");
        }
    }
}