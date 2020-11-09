using System;
using System.Threading;
using Hangfire.Server;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
#pragma warning disable 618
    public class ServerTimeSyncManager : IBackgroundProcess, IServerComponent
    {
        private readonly FluentNHibernateJobStorage _storage;

        public ServerTimeSyncManager(FluentNHibernateJobStorage storage)
        {
            _storage = storage;
        }

        public void Execute(BackgroundProcessContext context)
        {
            Execute(context.CancellationToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var test = UtcDateHelper.GetUtcNow(_storage.SessionFactoryInfo.SessionFactory,
                _storage.SessionFactoryInfo.ProviderType);
            cancellationToken.WaitHandle.WaitOne(_storage.Options.DbmsTimeSyncInterval);
        }
#pragma warning restore 618
    }
}