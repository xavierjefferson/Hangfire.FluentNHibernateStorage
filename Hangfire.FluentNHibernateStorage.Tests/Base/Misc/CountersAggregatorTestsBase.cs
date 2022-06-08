using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class CountersAggregatorTestsBase : TestBase
    {
        protected CountersAggregatorTestsBase(TestDatabaseFixture fixture) : base(fixture)
        {
            _storage = GetStorage();
            _countersAggregator = new CountersAggregator(_storage);
        }


        private readonly FluentNHibernateJobStorage _storage;
        private readonly CountersAggregator _countersAggregator;

        [Fact]
        public void CountersAggregatorExecutesProperly()
        {
            UseSession(_storage, sessionWrapper =>
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