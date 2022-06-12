using Hangfire.FluentNHibernateStorage.Tests.Base.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.MySql.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.MySql.JobQueue
{
    [Xunit.Collection(Constants.MySqlFixtureCollectionName)]
    public class
        MySqlJobQueueTests : JobQueueTestsBase
    {
        public MySqlJobQueueTests(MySqlTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}