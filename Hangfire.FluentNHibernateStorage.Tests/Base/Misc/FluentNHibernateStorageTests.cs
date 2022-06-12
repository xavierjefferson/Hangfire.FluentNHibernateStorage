using System;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class FluentNHibernateStorageTests : TestBase
    {
        public FluentNHibernateStorageTests(DatabaseFixtureBase fixture) : base(fixture)
        {
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInfoIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateJobStorage(null));

            Assert.Equal("info", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPersistenceConfigurerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateJobStorage(null, null));

            Assert.Equal("persistenceConfigurer", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = GetStorage();

            var components = storage.GetBackgroundProcesses();
            Assert.True(components.OfType<ExpirationManager>().Any());
            Assert.True(components.OfType<ServerTimeSyncManager>().Any());
            Assert.True(components.OfType<CountersAggregator>().Any());
        }


        [Fact]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = GetStorage();

            using (var connection = storage.GetConnection())
            {
                Assert.NotNull(connection);
            }
        }

        [Fact]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = GetStorage();

            var api = storage.GetMonitoringApi();
            Assert.NotNull(api);
        }
    }
}