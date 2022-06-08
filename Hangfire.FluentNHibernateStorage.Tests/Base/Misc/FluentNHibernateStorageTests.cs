using System;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class FluentNHibernateStorageTests<T, U> : TestBase<T, U> where T : IDbProvider, new() where U : TestDatabaseFixture
    {
      

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
            WithCleanTables(storage, s =>
            {
                var components = s.GetBackgroundProcesses();
                Assert.True(components.OfType<ExpirationManager>().Any());
            });
        }


        [Fact]
        public void GetConnection_ReturnsNonNullInstance()
        {
            var storage = GetStorage();
            WithCleanTables(storage, s =>
            {
                using (var connection = s.GetConnection())
                {
                    Assert.NotNull(connection);
                }
            });
        }

        [Fact]
        public void GetMonitoringApi_ReturnsNonNullInstance()
        {
            var storage = GetStorage();
            WithCleanTables(storage, s =>
            {
                var api = s.GetMonitoringApi();
                Assert.NotNull(api);
            });
        }
    }
}