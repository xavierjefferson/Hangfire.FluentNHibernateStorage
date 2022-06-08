using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class CountersAggregatorTestsBase<T, U> : TestBase<T, U> where T : IDbProvider, new() where U : TestDatabaseFixture
    {
        protected CountersAggregatorTestsBase()
        {
            _storage = GetStorage();
            _countersAggregator = new CountersAggregator(_storage);
        }


        private readonly FluentNHibernateJobStorage _storage;
        private readonly CountersAggregator _countersAggregator;

        [Fact]
        public void CountersAggregatorExecutesProperly()
        {
            WithCleanTables(_storage, sessionWrapper =>
            {
                //Arrange
                sessionWrapper.Insert(new _Counter {Key = "key", Value = 1, ExpireAt = _storage.UtcNow.AddHours(1)});


                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                _countersAggregator.Execute(cts.Token);

                // Assert
                Assert.Equal(1, sessionWrapper.Query<_AggregatedCounter>().Count());
            });
        }
    }
}