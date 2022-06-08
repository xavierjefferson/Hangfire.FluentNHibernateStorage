using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateStorageConnectionTests : FluentNHibernateStorageConnectionTestsBase
    {
        public SqlCeFluentNHibernateStorageConnectionTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}