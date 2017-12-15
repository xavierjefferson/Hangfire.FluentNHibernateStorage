using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class SessionWrapper : SessionWrapperBase, IWrappedSession
    {
        private readonly ISession _session;

        public SessionWrapper(ISession session)
        {
            this._session = session;
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

        public override void Dispose()
        {
            _session?.Dispose();
            base.Dispose();
        }
    }
}