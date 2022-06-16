using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernate.SampleStuff;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class SignalRSink<THub, T> : ILogEventSink
        where THub : Hub<T>
        where T : class, IHub
    {
        private readonly string[] _excludedConnectionIds;
        private readonly IFormatProvider _formatProvider;
        private readonly string[] _groups;

        private readonly IServiceProvider _serviceProvider;
        private readonly string[] _userIds;
        private IHubContext<THub, T> _hubContext;

        public SignalRSink(
            IFormatProvider formatProvider,
            IServiceProvider serviceProvider = null,
            string[] groups = null,
            string[] userIds = null,
            string[] excludedConnectionIds = null)
        {
            _formatProvider = formatProvider;

            _serviceProvider = serviceProvider;
            _groups = groups ?? new string[0];
            _userIds = userIds ?? new string[0];
            _excludedConnectionIds = excludedConnectionIds ?? new string[0];
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));
            if (_hubContext == null)
                _hubContext = _serviceProvider.GetRequiredService<IHubContext<THub, T>>();
            var objList = new List<T>();
            if (_groups.Any())
                objList.Add(_hubContext.Clients.Groups(_groups.Except(_excludedConnectionIds).ToArray()));
            if (_userIds.Any())
                objList.Add(_hubContext.Clients.Users(_userIds.Except(_excludedConnectionIds).ToArray()));
            if (!_groups.Any() && !_userIds.Any())
                objList.Add(_hubContext.Clients.AllExcept(_excludedConnectionIds));
            foreach (var obj in objList) obj.SendLogAsObject(new LogItem(logEvent));
        }
    }
}