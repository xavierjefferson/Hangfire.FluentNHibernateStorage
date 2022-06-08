using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class SqlCeFluentNHibernateStorageTests : FluentNHibernateStorageTests
    {
        public SqlCeFluentNHibernateStorageTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}