using System.Data;
using System.Linq;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper : IWrappedSession
    {
        private readonly IStatelessSession _session;

        public StatelessSessionWrapper(IStatelessSession s, IPersistenceConfigurer cfg) : base(cfg)
        {
            _session = s;
        }

        public ITransaction BeginTransaction(IsolationLevel iso)
        {
            return _session.BeginTransaction(iso);
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

        public IQuery CreateQuery(string queryString)
        {
            return _session.CreateQuery(queryString);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}