using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateJobStorageTestWrapper
    {
        private readonly FluentNHibernateJobStorage _storage;

        public FluentNHibernateJobStorageTestWrapper(FluentNHibernateJobStorage storage)
        {
            _storage = storage;
        }

        public void TruncateAllHangfireTables()
        {
            _storage.UseStatelessSession(session =>
            {
                session.DeleteAll<_List>();
                session.DeleteAll<_Hash>();
                session.DeleteAll<_Set>();
                session.DeleteAll<_Server>();
                session.DeleteAll<_JobQueue>();
                session.DeleteAll<_JobParameter>();
                session.DeleteAll<_JobState>();
                session.DeleteAll<_Job>();
                session.DeleteAll<_Counter>();
                session.DeleteAll<_AggregatedCounter>();
                session.DeleteAll<_DistributedLock>();
            });
        }

        public int ExecuteHqlQuery(string query)
        {
            return _storage.UseStatelessSession(session => { return session.CreateQuery(query).ExecuteUpdate(); });
        }
    }
}