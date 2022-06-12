using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    [Collection(Constants.SqlCeFixtureCollectionName)]
 
    public class
        SqlCeFetchedJobTests : FetchedJobTestsBase
    {
        public SqlCeFetchedJobTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}