using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.Monitoring
{
    [Xunit.Collection(Constants.MySqlFixtureCollectionName)]
    public class
        MySqlMonitoringApiTests : MonitoringApiTestsBase
    {
        public MySqlMonitoringApiTests(MySqlTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}