using System;
using System.Threading;
using Hangfire.Server;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class ServerTimeSyncManager : IBackgroundProcess, IServerComponent
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
            Execute(context.CancellationToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            _storage.RefreshUtcOffset();
            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }
#pragma warning restore 618
    }
}