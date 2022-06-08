using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Monitoring
{
    [Xunit.Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteFluentNHibernateMonitoringApiTests : FluentNHibernateMonitoringApiTestsBase
    {
        public SqliteFluentNHibernateMonitoringApiTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}