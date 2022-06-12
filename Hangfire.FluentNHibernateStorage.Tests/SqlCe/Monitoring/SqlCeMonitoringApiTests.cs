using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Monitoring
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeMonitoringApiTests : MonitoringApiTestsBase
    {
        public SqlCeMonitoringApiTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}