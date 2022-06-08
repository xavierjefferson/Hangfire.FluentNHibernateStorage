using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class SqlCeCounterAggregatorTests : CountersAggregatorTestsBase
    {
        public SqlCeCounterAggregatorTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}