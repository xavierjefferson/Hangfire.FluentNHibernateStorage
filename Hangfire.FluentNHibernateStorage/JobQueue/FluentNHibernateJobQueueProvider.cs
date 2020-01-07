using System;

namespace Hangfire.FluentNHibernateStorage.JobQueue
{
    internal class FluentNHibernateJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly IPersistentJobQueue _jobQueue;
        private readonly IPersistentJobQueueMonitoringApi _monitoringApi;

        public FluentNHibernateJobQueueProvider(FluentNHibernateJobStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            _jobQueue = new FluentNHibernateJobQueue(storage);
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