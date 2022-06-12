using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.SqlCe.Fixtures;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    [Xunit.Collection(Constants.SqlCeFixtureCollectionName)]
    public class
        SqlCeFluentNHibernateWriteOnlyTransactionTests : FluentNHibernateWriteOnlyTransactionTestsBase
    {
        public SqlCeFluentNHibernateWriteOnlyTransactionTests(SqlCeTestDatabaseFixture fixture) : base(fixture)
        {
        }
    }
}