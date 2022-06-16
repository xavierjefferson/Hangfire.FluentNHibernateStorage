using System.Collections.Generic;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public interface ILogPersistenceService
    {
        List<LogItem> GetRecent();
        void Insert(LogEvent l);
    }
}