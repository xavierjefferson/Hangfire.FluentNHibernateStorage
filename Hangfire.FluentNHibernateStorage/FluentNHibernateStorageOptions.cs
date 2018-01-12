using System;
using System.Transactions;
using Snork.FluentNHibernateTools;

namespace Hangfire.FluentNHibernateStorage
{
    public class FluentNHibernateStorageOptions:FluentNHibernatePersistenceBuilderOptions
    {
        private TimeSpan _queuePollInterval;

        public FluentNHibernateStorageOptions()
        {
            TransactionIsolationLevel = IsolationLevel.ReadCommitted;
            QueuePollInterval = TimeSpan.FromSeconds(15);
            JobExpirationCheckInterval = TimeSpan.FromHours(1);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);
            PrepareSchemaIfNecessary = true;
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

       

        public bool PrepareSchemaIfNecessary { get; set; }

        public TimeSpan JobExpirationCheckInterval { get; set; }
        public TimeSpan CountersAggregateInterval { get; set; }

        public int? DashboardJobListLimit { get; set; }
        public TimeSpan TransactionTimeout { get; set; }

        [Obsolete(
            "Does not make sense anymore. Background jobs re-queued instantly even after ungraceful shutdown now. Will be removed in 2.0.0.")]
        public TimeSpan InvisibilityTimeout { get; set; }
    }
}