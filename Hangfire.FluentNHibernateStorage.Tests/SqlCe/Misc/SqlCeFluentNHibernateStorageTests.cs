using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;

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