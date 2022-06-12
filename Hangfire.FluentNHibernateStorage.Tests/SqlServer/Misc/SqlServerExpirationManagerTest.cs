using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlServer.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlServer.Misc
{
    [Xunit.Collection(Constants.SqlServerFixtureCollectionName)]
    public class SqlServerExpirationManagerTest : ExpirationManagerTestsBase
    {
        public SqlServerExpirationManagerTest(SqlServerTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}