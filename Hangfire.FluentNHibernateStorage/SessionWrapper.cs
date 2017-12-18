using System.Data;
using System.Linq;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class SessionWrapper :  IWrappedSession
    {
        private readonly ISession _session;

        public SessionWrapper(ISession session, IPersistenceConfigurer pcf) : base(pcf)
        {
            _session = session;
        }

        public ITransaction BeginTransaction(IsolationLevel iso)
        {
            return _session.BeginTransaction(iso);
        }

        public ITransaction BeginTransaction()
        {
            return _session.BeginTransaction();
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