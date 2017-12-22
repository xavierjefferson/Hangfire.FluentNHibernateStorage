using System;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
    public class ServerTimeSyncManager : IBackgroundProcess
    {
        private readonly TimeSpan _checkInterval;
        private readonly FluentNHibernateJobStorage _storage;

        public ServerTimeSyncManager(FluentNHibernateJobStorage storage, TimeSpan checkInterval)
        {
            _storage = storage;
            _checkInterval = checkInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var cancellationToken = context.CancellationToken;
            _storage.RefreshUtcOffset();
            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }
    }
}