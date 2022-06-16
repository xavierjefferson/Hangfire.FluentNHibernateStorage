using System;
using Hangfire.FluentNHibernate.SampleStuff;
using Serilog.Core;
using Serilog.Events;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class SqliteSink : ILogEventSink

    {
        private readonly ILogPersistenceService _logPersistenceService;

        public SqliteSink(
            ILogPersistenceService logPersistenceService)
        {
            _logPersistenceService = logPersistenceService;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));
            _logPersistenceService.Insert(logEvent);
        }
    }
}