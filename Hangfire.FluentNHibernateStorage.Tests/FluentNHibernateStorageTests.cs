using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class FluentNHibernateStorageTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly FluentNHibernateStorageOptions _options;

        public FluentNHibernateStorageTests()
        {
            _options = new FluentNHibernateStorageOptions {PrepareSchemaIfNecessary = false};
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionStringIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateStorage(null));

            Assert.Equal("nameOrConnectionString", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => FluentNHibernateStorageFactory.ForMySQL(System.Configuration.ConfigurationManager.ConnectionString["hello"]), null);

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        [CleanDatabase]
        public void Ctor_CanCreateSqlServerStorage_WithExistingConnection()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var storage = new FluentNHibernateStorage(connection);

                Assert.NotNull(storage);
            }
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
        [CleanDatabase]
        public void GetComponents_ReturnsAllNeededComponents()
        {
            var storage = CreateStorage();

            var components = storage.GetComponents();

            var componentTypes = components.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(ExpirationManager), componentTypes);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            var api = storage.GetMonitoringApi();
            Assert.NotNull(api);
        }

        private FluentNHibernateStorage CreateStorage()
        {
            return new FluentNHibernateStorage(
                ConnectionUtils.GetConnectionString(),
                _options);
        }
    }
}