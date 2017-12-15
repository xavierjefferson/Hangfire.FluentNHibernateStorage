using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper : SessionWrapperBase, IWrappedSession
    {
        private readonly IStatelessSession s;

        public StatelessSessionWrapper(IStatelessSession s)
        {
            this.s = s;
        }

        public ITransaction BeginTransaction()
        {
            return s.BeginTransaction();
        }

        public void Insert(object x)
        {
            s.Insert(x);
        }

        public void Update(object x)
        {
            s.Update(x);
        }

        public void Flush()
        {
        }

        public IQueryable<X> Query<X>()
        {
            return s.Query<X>();
        }

        public IQuery CreateQuery(string queryString)
        {
            return s.CreateQuery(queryString);
        }

        public override void Dispose()
        {
            s?.Dispose();
            base.Dispose();
        }
    }
}