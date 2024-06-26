﻿using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Linq;

namespace Hangfire.FluentNHibernateStorage
{
    public class StatelessSessionWrapper : IDisposable
    {
        private readonly IStatelessSession _session;
        private bool _flushed;

        public StatelessSessionWrapper(IStatelessSession session, FluentNHibernateJobStorage storage)
        {
            _session = session;
            Storage = storage;
        }

        public FluentNHibernateJobStorage Storage { get; }

        public void Flush()
        {
            _session.GetSessionImplementation().Flush();
        }
        public void Dispose()
        {
            if (_session != null)
            {
                if (!_flushed)
                {
                    Flush();
                    _flushed = true;
                }

                _session.Dispose();
            }
        }

        public int DeleteAll<T>()
        {
            return _session.Query<T>().Delete();
        }

        public IQueryable<T> Query<T>()
        {
            return _session.Query<T>();
        }

        public IQuery CreateQuery(string queryString)
        {
            return _session.CreateQuery(queryString);
        }

        public void Insert<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var item in entities) _session.Insert(item);
            Flush();
        }

        public void Insert(object entity)
        {
            _session.Insert(entity);
            Flush();
        }

        public void Update(object entity)
        {
            _session.Update(entity);
            Flush();
        }
    }
}