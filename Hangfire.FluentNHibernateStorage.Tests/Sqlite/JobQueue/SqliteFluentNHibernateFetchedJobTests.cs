using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.JobQueue
{
    public class SqliteFluentNHibernateFetchedJobTests : FluentNHibernateFetchedJobTestsBase<SqliteProvider, SqliteDatabaseFixture>
    {
    }
}