using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.JobQueue
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateJobQueueTests : FluentNHibernateJobQueueTestsBase
    {
        public SqlCeFluentNHibernateJobQueueTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}