using Serilog.Core;

namespace Hangfire.FluentNHibernate.WinformsApplication
{
    public interface ILogEventEmitterService : ILogEventSink
    {
        event OnEmitHandler OnEmit;
    }
}