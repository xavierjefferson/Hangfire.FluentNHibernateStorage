using System;
using System.Transactions;
using Newtonsoft.Json;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorageOptions : FluentNHibernatePersistenceBuilderOptions
    {
        internal const string DefaultTablePrefix = "Hangfire_";
        private TimeSpan _countersAggregateInterval;
        private int? _dashboardJobListLimit;
        private TimeSpan _dbmsTimeSyncInterval;
        private TimeSpan _deadlockRetryInterval;
        private TimeSpan _distributedLockPollInterval;
        private TimeSpan _invisibilityTimeout;

        private TimeSpan _jobExpirationCheckInterval;
        private TimeSpan _jobQueueDistributedLockTimeout;
        private TimeSpan _queuePollInterval;

        private IObjectRenamer _objectRenamer;
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
            DbmsTimeSyncInterval = TimeSpan.FromMinutes(5);
            TablePrefix = DefaultTablePrefix;
        }

        /// <summary>
        ///     During a distributed lock acquisition, determines how often will Hangfire poll against the database while it waits.
        ///     Must be a positive timespan.
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
        ///     When the database encounters a deadlock state, how long to wait before retrying.  Must be a positive timespan.
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
        ///     How long to wait to get a distributed lock for the job queue.  Must be a positive timespan.
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
        ///     How long a job can run before Hangfire tries to re-queue it.  Must be a positive timespan.
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

        /// <summary>
        ///     For database operations specific to this provider, determines what level of access other transactions have to
        ///     volatile data before a transaction completes.
        /// </summary>
        public IsolationLevel TransactionIsolationLevel { get; set; }

        /// <summary>
        ///     How often this provider will check for new jobs and kick them off.  Must be a positive timespan.
        /// </summary>
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

        /// <summary>
        ///     How often this library invokes your DBMS's GetDate (or similar) function for the purpose of inserting timestamps
        ///     into the database.  Because of the ORM
        ///     approach, table insertions can't invoke server-side date functions directly.  Must be a positive timespan.
        /// </summary>
        public TimeSpan DbmsTimeSyncInterval
        {
            get => _dbmsTimeSyncInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(DbmsTimeSyncInterval));
                _dbmsTimeSyncInterval = value;
            }
        }

        /// <summary>
        ///     How often this provider will check for expired jobs and delete them from the database.  Must be a positive
        ///     timespan.
        /// </summary>
        public TimeSpan JobExpirationCheckInterval
        {
            get => _jobExpirationCheckInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(JobExpirationCheckInterval));
                _jobExpirationCheckInterval = value;
            }
        }

        /// <summary>
        ///     How often this provider will aggregate the job data to display it in the user interface.  This aggregation saves on
        ///     table space and generally improves performance of the UI.  Must be a positive timespan.
        /// </summary>
        public TimeSpan CountersAggregateInterval
        {
            get => _countersAggregateInterval;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(CountersAggregateInterval));
                _countersAggregateInterval = value;
            }
        }

        /// <summary>
        ///     The maximum number of jobs to show in the Hangfire dashboard.  Use null to show all jobs, or a positive integer.
        /// </summary>
        public int? DashboardJobListLimit
        {
            get => _dashboardJobListLimit;
            set
            {
                if (value != null)
                    ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(DashboardJobListLimit));
                _dashboardJobListLimit = value;
            }
        }

        /// <summary>
        ///     The maximum time span of transactions containing internal database operation for this provider.  Must be a positive
        ///     integer.
        /// </summary>
        public TimeSpan TransactionTimeout
        {
            get => _transactionTimeout;
            set
            {
                ArgumentHelper.ThrowIfValueIsNotPositive(value, nameof(TransactionTimeout));
                _transactionTimeout = value;
            }
        }

        public string TablePrefix
        {
            get => (ObjectRenamer as PrefixRenamer)?.Prefix;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(string.Format("{0} cannot be null or blank", nameof(TablePrefix)));
                _objectRenamer = new PrefixRenamer(value);
            }
        }

        [JsonIgnore]
        public override IObjectRenamer ObjectRenamer
        {
            get => _objectRenamer;
            set => throw new ArgumentException(string.Format("{0} cannot be set in this class", nameof(ObjectRenamer)));
        }
    }

    internal class PrefixRenamer : IObjectRenamer
    {
        public PrefixRenamer(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }

        public string Rename(ObjectTypeEnum type, string name)
        {
            if (Prefix.Equals(FluentNHibernateStorageOptions.DefaultTablePrefix))
            {
                switch (type)
                {
                    case ObjectTypeEnum.Table:
                        return string.Concat(Prefix, name);
                    default:
                        return name;
                }
            }
            else
            {
                return string.Concat(Prefix, name);
            }
           
        }
    }
}