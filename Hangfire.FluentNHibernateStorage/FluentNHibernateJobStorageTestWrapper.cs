namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateJobStorageTestWrapper
    {
        private readonly FluentNHibernateJobStorage _storage;

        public FluentNHibernateJobStorageTestWrapper(FluentNHibernateJobStorage storage)
        {
            _storage = storage;
        }

        public int ExecuteHqlQuery(string query)
        {
            return _storage.UseStatelessSession(session => { return session.CreateQuery(query).ExecuteUpdate(); });
        }
    }
}