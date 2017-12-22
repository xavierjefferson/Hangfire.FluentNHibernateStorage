using System;
using System.Linq;
using System.Transactions;
using Hangfire.FluentNHibernateStorage;
using Hangfire.FluentNHibernateStorage.Tests;
using Xunit;

namespace Hangfire.FluentNHibernateJobStorage.Tests
{
    public class FluentNHibernateStorageTests : IClassFixture<TestDatabaseFixture>
    {
        public FluentNHibernateStorageTests()
        {
            _options = new FluentNHibernateStorageOptions {PrepareSchemaIfNecessary = false};
        }

        private readonly FluentNHibernateStorageOptions _options;

        private FluentNHibernateStorage.FluentNHibernateJobStorage CreateStorage()
        {
            return ConnectionUtils.CreateStorage(_options);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPersistenceConfigurerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateStorage.FluentNHibernateJobStorage(null));

            Assert.Equal("nameOrConnectionString", exception.ParamName);
        }

        [Fact]
        [CleanDatabase]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = CreateStorage();

            var components = storage.GetComponents();

            var componentTypes = components.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(ExpirationManager), componentTypes);
        }


        [Fact]
        [CleanDatabase]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            using (var connection = (FluentNHibernateJobStorageConnection) storage.GetConnection())
            {
                Assert.NotNull(connection);
            }
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            var api = storage.GetMonitoringApi();
            Assert.NotNull(api);
        }
    }
}