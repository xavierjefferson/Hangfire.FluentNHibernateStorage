using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class CountersAggregatorTestsBase : TestBase
    {
        protected CountersAggregatorTestsBase(DatabaseFixtureBase fixture) : base(fixture)
        {
        }


        [Fact]
        public void CountersAggregatorExecutesProperly()
        {
            UseJobStorageConnectionWithSession((sessionWrapper, connection) =>
            {
                //Arrange
                sessionWrapper.Insert(new _Counter
                    {Key = "key", Value = 1, ExpireAt = connection.Storage.UtcNow.AddHours(1)});


                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                var countersAggregator = new CountersAggregator(connection.Storage);
                countersAggregator.Execute(cts.Token);

                // Assert
                Assert.Equal(1, sessionWrapper.Query<_AggregatedCounter>().Count());
            });
        }
    }
}