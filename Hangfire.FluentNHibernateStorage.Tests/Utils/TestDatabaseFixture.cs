using System;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Tests.Providers;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class SqlCeDatabaseFixture : TestDatabaseFixture
    {
        private SqlCeProvider ss = new SqlCeProvider();
        public override void CreateDatabase()
        {
            ss.CreateDatabase();

        }

        public override void DestroyDatabase()
        {

            ss.DestroyDatabase();
        }
    }

    public class SqliteDatabaseFixture : TestDatabaseFixture
    {
        private SqliteProvider ss = new SqliteProvider();
        public override void CreateDatabase()
        {
            ss.CreateDatabase();
             
        }

        public override void DestroyDatabase()
        {
          
            ss.DestroyDatabase();
        }
    }
    public abstract class TestDatabaseFixture : IDisposable
    {
        private static readonly object GlobalLock = new object();

        public abstract void CreateDatabase();
        public abstract void DestroyDatabase();

        public TestDatabaseFixture()
        {
            Monitor.Enter(GlobalLock);
            CreateDatabase();
        }

        public void Dispose()
        {
            Monitor.Exit(GlobalLock);
            DestroyDatabase();
        }


    }
}