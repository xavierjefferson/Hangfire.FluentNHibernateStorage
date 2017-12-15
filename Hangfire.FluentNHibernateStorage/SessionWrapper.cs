using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class SessionWrapper : SessionWrapperBase, IWrappedSession
    {
        private readonly ISession s;

        public SessionWrapper(ISession s)
        {
            this.s = s;
        }

        public ITransaction BeginTransaction()
        {
            return s.BeginTransaction();
        }

        public IQueryable<T> Query<T>()
        {
            return s.Query<T>();
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

        public override void Dispose()
        {
            s?.Dispose();
            base.Dispose();
        }
    }
}