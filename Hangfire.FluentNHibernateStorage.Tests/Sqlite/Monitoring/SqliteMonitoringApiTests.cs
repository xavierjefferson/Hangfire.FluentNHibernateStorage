using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.Sqlite.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Monitoring
{
    [Xunit.Collection(Constants.SqliteFixtureCollectionName)]
    public class SqliteMonitoringApiTests : MonitoringApiTestsBase
    {
        public SqliteMonitoringApiTests(SqliteTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}