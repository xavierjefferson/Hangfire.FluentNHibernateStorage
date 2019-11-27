using System;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal class FluentNHibernateJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly IPersistentJobQueue _jobQueue;
        private readonly IPersistentJobQueueMonitoringApi _monitoringApi;

        public FluentNHibernateJobQueueProvider(FluentNHibernateJobStorage storage,
            FluentNHibernateStorageOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _jobQueue = new FluentNHibernateJobQueue(storage, options);
            _monitoringApi = new FluentNHibernateJobQueueMonitoringApi(storage);
        }

        public IPersistentJobQueue GetJobQueue()
        {
            return _jobQueue;
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi()
        {
            return _monitoringApi;
        }
    }
}