using System;
using System.Transactions;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorageOptions : FluentNHibernatePersistenceBuilderOptions
    {
        private TimeSpan _countersAggregateInterval;
        private int? _dashboardJobListLimit;
        private TimeSpan _deadlockRetryInterval;
        private TimeSpan _jobQueueDistributedLockTimeout;
        private TimeSpan _invisibilityTimeout;

        private TimeSpan _jobExpirationCheckInterval;
        private TimeSpan _queuePollInterval;
        private TimeSpan _distributedLockPollInterval;

        private TimeSpan _transactionTimeout;

        public FluentNHibernateStorageOptions()
        {
            TransactionIsolationLevel = IsolationLevel.Serializable;
            QueuePollInterval = TimeSpan.FromSeconds(15);
            JobExpirationCheckInterval = TimeSpan.FromHours(1);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            UpdateSchema = true;
            DashboardJobListLimit = 50000;
            TransactionTimeout = TimeSpan.FromMinutes(1);
            InvisibilityTimeout = TimeSpan.FromMinutes(15);
            JobQueueDistributedLockTimeout = TimeSpan.FromMinutes(1);
            DistributedLockPollInterval = TimeSpan.FromMilliseconds(100);
            DeadlockRetryInterval = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        ///     During a distributed lock acquisition, determines how often will Hangfire poll against the database while it waits.
        /// </summary>
        public TimeSpan DistributedLockPollInterval
        {
            get => _distributedLockPollInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(DistributedLockPollInterval));
                _distributedLockPollInterval = value;
            }
        }

        /// <summary>
        ///     When the database encounters a deadlock state, how long to wait before retrying
        /// </summary>
        public TimeSpan DeadlockRetryInterval
        {
            get => _deadlockRetryInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(DeadlockRetryInterval));
                _deadlockRetryInterval = value;
            }
        }

        /// <summary>
        ///     How long to wait to get a distributed lock for the job queue
        /// </summary>
        public TimeSpan JobQueueDistributedLockTimeout
        {
            get => _jobQueueDistributedLockTimeout;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(JobQueueDistributedLockTimeout));
                _jobQueueDistributedLockTimeout = value;
            }
        }

        /// <summary>
        ///     How long a job can run before Hangfire tries to re-queue it
        /// </summary>
        public TimeSpan InvisibilityTimeout
        {
            get => _invisibilityTimeout;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(InvisibilityTimeout));
                _invisibilityTimeout = value;
            }
        }

        public IsolationLevel? TransactionIsolationLevel { get; set; }

        public TimeSpan QueuePollInterval
        {
            get => _queuePollInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(QueuePollInterval));
                _queuePollInterval = value;
            }
        }

        /// <summary>
        ///     Create the schema if it doesn't already exist.
        /// </summary>
        [Obsolete("Use property " + nameof(UpdateSchema) + ".")]
        public bool PrepareSchemaIfNecessary
        {
            get => UpdateSchema;
            set => UpdateSchema = value;
        }

        public TimeSpan JobExpirationCheckInterval
        {
            get => _jobExpirationCheckInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(JobExpirationCheckInterval));
                _jobExpirationCheckInterval = value;
            }
        }

        public TimeSpan CountersAggregateInterval
        {
            get => _countersAggregateInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(CountersAggregateInterval));
                _countersAggregateInterval = value;
            }
        }

        public int? DashboardJobListLimit
        {
            get => _dashboardJobListLimit;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(DashboardJobListLimit));
                _dashboardJobListLimit = value;
            }
        }

        public TimeSpan TransactionTimeout
        {
            get => _transactionTimeout;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(TransactionTimeout));
                _transactionTimeout = value;
            }
        }
    }
}