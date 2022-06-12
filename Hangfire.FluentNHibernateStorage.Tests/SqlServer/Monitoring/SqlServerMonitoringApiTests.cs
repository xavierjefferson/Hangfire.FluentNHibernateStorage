using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Monitoring
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class
        SqlServerMonitoringApiTests : MonitoringApiTestsBase
    {
        public SqlServerMonitoringApiTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}