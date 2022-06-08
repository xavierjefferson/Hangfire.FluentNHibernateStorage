using Hangfire.FluentNHibernateStorage.Entities;
using NHibernate.Type;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : Int32IdMapBase<_Job>
    {
        public _JobMap()
        {
            Table("Job");
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
            this.MapCreatedAt();
            this.MapExpireAt();
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