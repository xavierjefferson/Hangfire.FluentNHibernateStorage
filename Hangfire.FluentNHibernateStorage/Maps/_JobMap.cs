using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : Int64IdMapBase<_Job>
    {
        public _JobMap()
        {
            Table("Hangfire_Job".WrapObjectName());
            LazyLoad();
            Map(i => i.LastStateChangedAt).Column("`LastStateChangedAt`").Nullable();
            Map(i => i.StateData).Column("`StateData`").Length(Constants.StateDataLength).Nullable();
            Map(i => i.InvocationData)
                .Column("InvocationData".WrapObjectName())
                .Length(Constants.VarcharMaxLength)
                .Not.Nullable();
            Map(i => i.Arguments)
                .Column("Arguments".WrapObjectName())
                .Length(Constants.VarcharMaxLength)
                .Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.ColumnNames.CreatedAt.WrapObjectName()).Not.Nullable();
            Map(i => i.ExpireAt).Column("ExpireAt".WrapObjectName()).Nullable();
            var jobId = Constants.ColumnNames.JobId.WrapObjectName();
            HasMany(i => i.Parameters).KeyColumn(jobId).Cascade.All();
            HasMany(i => i.History).KeyColumn(jobId).Cascade.All();

            Map(i => i.StateName).Column("StateName".WrapObjectName()).Length(Constants.StateNameLength).Nullable();
            Map(i => i.StateReason)
                .Column("StateReason".WrapObjectName())
                .Length(Constants.StateReasonLength)
                .Nullable();
        }
    }
}