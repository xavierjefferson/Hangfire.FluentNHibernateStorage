using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernate.SampleStuff;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class LogItemMap : ClassMap<LogItem>
    {
        public LogItemMap()
        {
            Table("Logs");
            Id(i => i.Id).Column("`id`").GeneratedBy.Assigned();
            Map(i => i.Timestamp).Nullable();
            Map(i => i.Level).Nullable().Length(10);
            Map(i => i.Exception).Nullable();
            Map(i => i.Message).Nullable();
            Map(i => i.Properties).Nullable();
            Map(i => i.dt).Not.Nullable().Index("abc");
        }
    }
}