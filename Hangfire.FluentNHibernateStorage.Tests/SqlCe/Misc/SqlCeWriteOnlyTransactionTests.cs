using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeWriteOnlyTransactionTests : WriteOnlyTransactionTestsBase
    {
        public SqlCeWriteOnlyTransactionTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}