using System;
using System.Linq;
using Hangfire.Logging;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
   public  abstract class SessionWrapperBase : IDisposable
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static object mutex = new object();
        private static int count = 0;
        public SessionWrapperBase()
        {
            lock (mutex)
            {
                count++;

                Logger.InfoFormat("+Session {0} {1}", this.GetHashCode(), count);
            }
        }

        public virtual void Dispose()
        {
            lock (mutex)
            {
                count--;

                Logger.InfoFormat("-Session {0} {1}", this.GetHashCode(), count);
            }
        }
    }
    public class SessionWrapper : SessionWrapperBase, IWrappedSession
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

        public override void Dispose()
        {
            s?.Dispose();
            base.Dispose();
        }
    }
}