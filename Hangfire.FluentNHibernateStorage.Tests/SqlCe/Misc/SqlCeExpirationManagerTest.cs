using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class SqlCeExpirationManagerTest : ExpirationManagerTestsBase
    {
        public SqlCeExpirationManagerTest(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}