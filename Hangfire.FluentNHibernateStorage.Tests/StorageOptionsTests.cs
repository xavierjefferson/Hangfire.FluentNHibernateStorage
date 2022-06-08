using System;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class StorageOptionsTests
    {
        [Fact]
        public void Ctor_SetsTheDefaultOptions()
        {
            var options = new FluentNHibernateStorageOptions();

            Assert.True(options.QueuePollInterval > TimeSpan.Zero);

            Assert.True(options.JobExpirationCheckInterval > TimeSpan.Zero);
            Assert.True(options.UpdateSchema);
        }

        [Fact]
        public void Set_QueuePollInterval_SetsTheValue()
        {
            var options = new FluentNHibernateStorageOptions();
            options.QueuePollInterval = TimeSpan.FromSeconds(1);
            Assert.Equal(TimeSpan.FromSeconds(1), options.QueuePollInterval);
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsEqualToZero()
        {
            var options = new FluentNHibernateStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.Zero);
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsNegative()
        {
            var options = new FluentNHibernateStorageOptions();
            Assert.Throws<ArgumentException>(
                () => options.QueuePollInterval = TimeSpan.FromSeconds(-1));
        }
    }
}