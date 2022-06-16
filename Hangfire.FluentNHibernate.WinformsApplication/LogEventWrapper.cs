using System;
using Hangfire.FluentNHibernate.SampleStuff;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.WinformsApplication
{
    public class LogEventWrapper
    {
        private readonly IFormatProvider _formatProvider;
        private readonly LogEvent _logEvent;

        public LogEventWrapper(LogEvent logEvent, IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
            _logEvent = logEvent;
        }

        public string Message => _logEvent.RenderMessage(_formatProvider);

        public string LoggerName => "tbd";

        public string ThreadName => _logEvent.Properties[ThreadIDEnricher.PropertyName].ToString();

        public LogEventLevel Level => _logEvent.Level;

        public string TimeStamp => _logEvent.Timestamp.DateTime.ToString("yyyy-MM-dd hh:mm:ss");

        public string Exception => _logEvent.Exception?.Message;
    }
}