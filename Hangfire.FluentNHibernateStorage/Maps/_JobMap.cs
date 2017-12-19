using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : IntIdMap<_Job>
    {
        public _JobMap()
        {
            Table("Hangfire_Job".WrapObjectName());
            LazyLoad();
            Map(i => i.LastStateChangedAt).Column("`LastStateChangedAt`").Nullable();
            Map(i => i.StateData).Column("`StateData`").Length(_JobStateMap.stateDataLength).Nullable();
            Map(i => i.InvocationData)
                .Column("InvocationData".WrapObjectName())
                .Length(Constants.VarcharMaxLength)
                .Not.Nullable();
            Map(i => i.Arguments)
                .Column("Arguments".WrapObjectName())
                .Length(Constants.VarcharMaxLength)
                .Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAt).Not.Nullable();
            Map(i => i.ExpireAt).Column("ExpireAt".WrapObjectName()).Nullable();
            HasMany(i => i.Parameters).KeyColumn(Constants.JobId).Cascade.All();
            HasMany(i => i.History).KeyColumn(Constants.JobId).Cascade.All();

            Map(i => i.StateName).Column("StateName".WrapObjectName()).Length(_JobStateMap.stateNameLength).Nullable();
            Map(i => i.StateReason).Column("StateReason".WrapObjectName()).Length(_JobStateMap.stateReasonLength)
                .Nullable();
        }
    }
}