using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobParameterMap : IntIdMap<_JobParameter>
    {
        public _JobParameterMap()
        {
            Table("`Hangfire_JobParameter`");
            References(i => i.Job).Column(Constants.JobIdColumnName).Not.Nullable().Cascade.Delete();
            Map(i => i.Name).Column("`Name`").Length(40).Not.Nullable();
            Map(i => i.Value).Column("`Value`").Length(Constants.VarcharMaxLength).Nullable();
        }
    }
}