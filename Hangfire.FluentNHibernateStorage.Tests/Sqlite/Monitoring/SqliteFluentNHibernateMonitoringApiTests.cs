using Hangfire.FluentNHibernateStorage.Tests.Base.Monitoring;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Monitoring
{
    public class SqliteFluentNHibernateMonitoringApiTests : FluentNHibernateMonitoringApiTestsBase<SqliteProvider, SqliteDatabaseFixture>
    {
    }
}