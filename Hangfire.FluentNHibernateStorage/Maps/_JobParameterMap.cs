using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobParameterMap : IntIdMap<_JobParameter>
    {
        public _JobParameterMap()
        {
            Table("Hangfire_JobParameter".WrapObjectName());
            References(i => i.Job).Column(Constants.JobId).Not.Nullable().Cascade.Delete();
            Map(i => i.Name).Column("Name".WrapObjectName()).Length(40).Not.Nullable();
            Map(i => i.Value).Column("Value".WrapObjectName()).Length(Constants.VarcharMaxLength).Nullable();
        }
    }
}