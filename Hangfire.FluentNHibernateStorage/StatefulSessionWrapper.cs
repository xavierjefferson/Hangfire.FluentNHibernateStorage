using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatefulSessionWrapper : SessionWrapperBase, IWrappedSession
    {
        private readonly ISession _session;

        public StatefulSessionWrapper(ISession session, ProviderTypeEnum type, FluentNHibernateJobStorage storage)
        {
            _session = session;
            ProviderType = type;
            Storage = storage;
        }


        public ITransaction BeginTransaction()
        {
            return _session.BeginTransaction();
        }

        public void Clear()
        {
            _session.Clear();
        }

        public ITransaction BeginTransaction(IsolationLevel level)
        {
            return _session.BeginTransaction(level);
        }

        public IQueryable<T> Query<T>()
        {
            return _session.Query<T>();
        }

        public override IQuery CreateQuery(string queryString)
        {
            return _session.CreateQuery(queryString);
        }

        public void Evict(object x)
        {
            _session.Evict(x);
            
        }

        public ISQLQuery CreateSqlQuery(string queryString)
        {
            return _session.CreateSQLQuery(queryString);
        }

        public void Insert(object x)
        {
            _session.Save(x);
        }

        public void Update(object x)
        {
            _session.Update(x);
        }

        public void Flush()
        {
            _session.Flush();
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}