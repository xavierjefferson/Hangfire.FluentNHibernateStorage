using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using NHibernate;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public class LogPersistenceService : ILogPersistenceService
    {
        private readonly ISessionFactory _sessionFactory;

        public LogPersistenceService(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public List<LogItem> GetRecent()
        {
            using (var statelessSession = _sessionFactory.OpenStatelessSession())
            {
                using (var transaction = statelessSession.BeginTransaction(IsolationLevel.Serializable))
                {
                    return statelessSession.Query<LogItem>().OrderBy(i => i.dt).ToList();
                }
            }
        }

        public void Insert(LogEvent logEvent)
        {
            using (var statelessSession = _sessionFactory.OpenStatelessSession())
            {
                statelessSession.Insert(new LogItem (logEvent));
            }
        }
    }
}