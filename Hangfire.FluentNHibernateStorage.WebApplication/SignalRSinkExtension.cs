using System;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public static class SignalRSinkExtension
    {
        public static LoggerConfiguration SignalRSink<THub, T>(
            this LoggerSinkConfiguration loggerConfiguration,
            LogEventLevel logEventLevel,
            IServiceProvider serviceProvider = null,
            IFormatProvider formatProvider = null,
            string[] groups = null,
            string[] userIds = null,
            string[] excludedConnectionIds = null,
            bool sendAsString = false)
            where THub : Hub<T>
            where T : class, IHub
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException(nameof(loggerConfiguration));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            return loggerConfiguration.Sink(
                new SignalRSink<THub, T>(formatProvider, serviceProvider, groups, userIds,
                    excludedConnectionIds), logEventLevel);
        }
    }
}