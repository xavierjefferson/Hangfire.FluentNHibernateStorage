using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _JobStateMap : IntIdMap<_JobState>
    {
        public const int stateReasonLength = 100;
        public const int stateDataLength = Constants.VarcharMaxLength;
        public const int stateNameLength = 20;
        public _JobStateMap()
        {
            Table("Hangfire_JobState".WrapObjectName());


            Map(i => i.Name).Column("Name".WrapObjectName()).Length(stateNameLength).Not.Nullable();
            
            Map(i => i.Reason).Column("Reason".WrapObjectName()).Length(stateReasonLength).Nullable();
            Map(i => i.Data).Column(Constants.Data).Length(stateDataLength).Nullable();
            Map(i => i.CreatedAt).Column(Constants.CreatedAt).Not.Nullable();
            
          
            References(i => i.Job).Column(Constants.JobId).Not.Nullable().Cascade.Delete();
        }
    }
}