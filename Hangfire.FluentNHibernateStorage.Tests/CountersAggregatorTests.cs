using System;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests;
using Xunit;

namespace Hangfire.FluentNHibernateJobStorage.Tests
{
    public class CountersAggregatorTests : IClassFixture<TestDatabaseFixture>, IDisposable
    {
        public CountersAggregatorTests()
        {
            _storage = ConnectionUtils.GetStorage();
            _countersAggregator = new CountersAggregator(_storage);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }

        private readonly FluentNHibernateStorage.FluentNHibernateJobStorage _storage;
        private readonly CountersAggregator _countersAggregator;

        [Fact]
        [CleanDatabase]
        public void CountersAggregatorExecutesProperly()
        {
            _storage.UseSession(connection =>
            {
                //Arrange
                connection.Insert(new _Counter {Key = "key", Value = 1, ExpireAt = _storage.UtcNow.AddHours(1)});
                connection.Flush();

                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                _countersAggregator.Execute(cts.Token);

                // Assert
                Assert.Equal(1, connection.Query<_AggregatedCounter>().Count());
            });
        }
    }
}