using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    [Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateFetchedJobTests : FluentNHibernateFetchedJobTestsBase
    {
        public SqlCeFluentNHibernateFetchedJobTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}