using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    public class SqlCeFluentNHibernateFetchedJobTests : FluentNHibernateFetchedJobTestsBase<SqlCeProvider, SqlCeDatabaseFixture>
    {
    }
}