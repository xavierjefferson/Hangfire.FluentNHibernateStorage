using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public class ThreadIDEnricher : ILogEventEnricher
    {
        public const string PropertyName = "ThreadID";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                PropertyName, Thread.CurrentThread.Name));
        }
    }
}