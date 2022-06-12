using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Misc
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class SqlServerCounterAggregatorTests : CountersAggregatorTestsBase
    {
        public SqlServerCounterAggregatorTests(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}