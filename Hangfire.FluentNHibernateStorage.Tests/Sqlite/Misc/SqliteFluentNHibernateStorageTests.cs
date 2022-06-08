using Hangfire.FluentNHibernateStorage.Tests.Base.Misc;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests.Sqlite.Misc
{
    public class SqliteFluentNHibernateStorageTests : FluentNHibernateStorageTests<SqliteProvider, SqliteDatabaseFixture>
    {
    }
}