using Serilog.Events;

namespace Hangfire.FluentNHibernate.WinformsApplication
{
    public delegate void OnEmitHandler(LogEvent logEvent);
}