using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.SqlCe.Misc
{
    public class SqlCeFluentNHibernateStorageConnectionTests : FluentNHibernateStorageConnectionTestsBase<SqlCeProvider, SqlCeDatabaseFixture>
    {
    }
}