using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper : SessionWrapperBase, IWrappedSession
    {
        private readonly IStatelessSession _session;

        public StatelessSessionWrapper(IStatelessSession session, ProviderTypeEnum type,
            FluentNHibernateJobStorage storage)
        {
            _session = session;
            ProviderType = type;
            Storage = storage;
        }
        public void Evict(object x)
        {
          

        }

        public void Clear()
        {
            
        }
        public ITransaction BeginTransaction(IsolationLevel level)
        {
            return _session.BeginTransaction(level);
        }


        public ISQLQuery CreateSqlQuery(string queryString)
        {
            return _session.CreateSQLQuery(queryString);
        }

        public ITransaction BeginTransaction()
        {
            return _session.BeginTransaction();
        }

        public void Insert(object x)
        {
            _session.Insert(x);
        }

        public void Update(object x)
        {
            _session.Update(x);
        }

        public void Flush()
        {
        }

        public IQueryable<T> Query<T>()
        {
            return _session.Query<T>();
        }

        public override IQuery CreateQuery(string queryString)
        {
            return _session.CreateQuery(queryString);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}