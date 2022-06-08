using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Monitoring
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateMonitoringApiTests : FluentNHibernateMonitoringApiTestsBase
    {
        public SqlCeFluentNHibernateMonitoringApiTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}