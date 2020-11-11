using System;
using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper : IDisposable
    {
        private readonly IStatelessSession _session;

        public StatelessSessionWrapper(IStatelessSession session, FluentNHibernateJobStorage storage)
        {
            _session = session;
            Storage = storage;
        }

        public FluentNHibernateJobStorage Storage { get; }


        public void Dispose()
        {
            _session?.Dispose();
        }


        protected void ReleaseUnmanagedResources()
        {
            _session?.Dispose();
        }

        public void DeleteAll<T>()
        {
            _session.Query<T>().Delete();
        }


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

        public void Insert(object x)
        {
            _session.Insert(x);
        }

        public void Update(object x)
        {
            _session.Update(x);
        }
    }
}