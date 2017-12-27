using System;
using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class SessionWrapper : IDisposable
    {
        private readonly ISession _session;

        public SessionWrapper(ISession session, ProviderTypeEnum type, FluentNHibernateJobStorage storage)
        {
            _session = session;
            ProviderType = type;
            Storage = storage;
        }

        public FluentNHibernateJobStorage Storage { get; protected set; }
        public ProviderTypeEnum ProviderType { get; protected set; }

        public IDbConnection Connection => _session.Connection;

        public void Dispose()
        {
            _session?.Dispose();
        }

        public void DeleteAll<T>()
        {
            ExecuteQuery(string.Format("delete from {0}", typeof(T).Name));
        }

        public int ExecuteQuery(string queryString)
        {
            return CreateQuery(queryString).ExecuteUpdate();
        }


        public ITransaction BeginTransaction()
        {
            return _session.BeginTransaction();
        }

        public void Clear()
        {
            _session.Clear();
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

        public void Evict(object x)
        {
            _session.Evict(x);
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
    }
}