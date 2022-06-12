using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;

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