using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class SessionWrapper : IWrappedSession
    {
        public ITransaction BeginTransaction()
        {
            return s.BeginTransaction();
        }
        public IQueryable<T> Query<T>()
        {
            return s.Query<T>();
        }

        private ISession s;
        public SessionWrapper(ISession s)
        {
            this.s = s;
        }

        public IQuery CreateQuery(string queryString)
        {
            return s.CreateQuery(queryString);
        }

        public void Insert(object x)
        {
            s.Save(x);
        }

        public void Update(object x)
        {
            s.Update(x);
        }

        public void Flush()
        {
            s.Flush();
        }

        public void Dispose()
        {
            s?.Dispose();
        }
    }
}