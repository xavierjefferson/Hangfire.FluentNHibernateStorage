using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatefulSessionWrapper : IWrappedSession
    {
        private readonly ISession _session;

        public StatefulSessionWrapper(ISession session, ProviderTypeEnum type)
        {
            _session = session;
            ProviderType = type;
        }

        public ProviderTypeEnum ProviderType { get; }

        public ITransaction BeginTransaction()
        {
            return _session.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel level)
        {
            return _session.BeginTransaction(level);
        }

        public IQueryable<T> Query<T>()
        {
            return _session.Query<T>();
        }

        public IQuery CreateQuery(string queryString)
        {
            return _session.CreateQuery(queryString);
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