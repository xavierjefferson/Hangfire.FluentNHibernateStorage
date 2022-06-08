using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Monitoring
{
    public class SqlCeFluentNHibernateMonitoringApiTests : FluentNHibernateMonitoringApiTestsBase<SqlCeProvider, SqlCeDatabaseFixture>
    {
    }
}