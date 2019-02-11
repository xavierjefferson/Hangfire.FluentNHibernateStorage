using System;
using System.Transactions;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorageOptions : FluentNHibernatePersistenceBuilderOptions
    {
        private TimeSpan _queuePollInterval;

        public FluentNHibernateStorageOptions()
        {
            TransactionIsolationLevel = IsolationLevel.Serializable;
            QueuePollInterval = TimeSpan.FromSeconds(15);
            JobExpirationCheckInterval = TimeSpan.FromHours(1);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            UpdateSchema = true;
            DashboardJobListLimit = 50000;
            TransactionTimeout = TimeSpan.FromMinutes(1);
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
        }

        public IsolationLevel? TransactionIsolationLevel { get; set; }

        public TimeSpan QueuePollInterval
        {
            get => _queuePollInterval;
            set
            {
                if (value == TimeSpan.Zero || value != value.Duration())
                {
                    var message = string.Format(
                        "The QueuePollInterval property value should be positive. Given: {0}.",
                        value);
                    throw new ArgumentException(message, "value");
                }

                _queuePollInterval = value;
            }
        }

        /// <summary>
        /// Create the schema if it doesn't already exist.
        /// </summary>
        [Obsolete("Use property " + nameof(UpdateSchema) + ".")]
        public bool PrepareSchemaIfNecessary
        {
            get => UpdateSchema;
            set => UpdateSchema = value;
        }

        public TimeSpan JobExpirationCheckInterval { get; set; }
        public TimeSpan CountersAggregateInterval { get; set; }

        public int? DashboardJobListLimit { get; set; }
        public TimeSpan TransactionTimeout { get; set; }

        [Obsolete(
            "Does not make sense anymore. Background jobs re-queued instantly even after ungraceful shutdown now. Will be removed in 2.0.0.")]
        public TimeSpan InvisibilityTimeout { get; set; }
    }
}