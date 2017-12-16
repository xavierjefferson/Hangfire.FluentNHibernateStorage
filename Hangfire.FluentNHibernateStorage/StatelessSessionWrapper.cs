using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper :  IWrappedSession
    {
        private readonly IStatelessSession _session;

        public StatelessSessionWrapper(IStatelessSession s)
        {
            this._session = s;
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