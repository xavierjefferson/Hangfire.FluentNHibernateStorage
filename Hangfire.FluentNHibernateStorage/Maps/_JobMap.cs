using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : IntIdMap<_Job>
    {
        public _JobMap()
        {
            Table("Hangfire_Job".WrapObjectName());
            LazyLoad();
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
            References(i => i.CurrentState).Column("StateId".WrapObjectName()).Cascade.All().Nullable();
        }
    }
}