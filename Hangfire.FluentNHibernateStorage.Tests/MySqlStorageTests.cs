using System;
using System.Linq;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class MySqlStorageTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly NHStorageOptions _options;

        public MySqlStorageTests()
        {
            _options = new NHStorageOptions {PrepareSchemaIfNecessary = false};
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionStringIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new NHStorage(null));

            Assert.Equal("nameOrConnectionString", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new NHStorage("hello", null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        [CleanDatabase]
        public void Ctor_CanCreateSqlServerStorage_WithExistingConnection()
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var storage = new NHStorage(connection);

                Assert.NotNull(storage);
            }
        }

        [Fact]
        [CleanDatabase]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = CreateStorage();
            using (var connection = (NHStorageConnection) storage.GetConnection())
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

        private NHStorage CreateStorage()
        {
            return new NHStorage(
                ConnectionUtils.GetConnectionString(),
                _options);
        }
    }
}