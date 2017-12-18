using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobMap : IntIdMap<_Job>
    {
        public _JobMap()
        {
            Table("`Hangfire_Job`");
            LazyLoad();
            Map(i => i.InvocationData).Column("`InvocationData`").Length(Constants.VarcharMaxLength).Not.Nullable();
            Map(i => i.Arguments).Column("`Arguments`").Length(Constants.VarcharMaxLength).Not.Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAtColumnName).Not.Nullable();
            Map(i => i.ExpireAt).Column("`ExpireAt`").Nullable();
            HasMany(i => i.Parameters).KeyColumn(Constants.JobIdColumnName).Cascade.Delete();
            HasMany(i => i.History).KeyColumn(Constants.JobIdColumnName).Cascade.Delete();
            References(i => i.CurrentState).Column("`StateId`").Cascade.None().Nullable();
        }
    }
}