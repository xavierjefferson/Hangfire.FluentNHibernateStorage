using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Hangfire.Server;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class ExpirationManagerTestsBase<T, U> : TestBase<T, U> where T : IDbProvider, new() where U : TestDatabaseFixture
    {
        protected ExpirationManagerTestsBase()
        {
            _storage = GetStorage();

            _storage.Options.JobExpirationCheckInterval = TimeSpan.Zero;
            var cts = new CancellationTokenSource();
            _context = new BackgroundProcessContext("dummy", _storage, new Dictionary<string, object>(), Guid.NewGuid(),
                cts.Token, cts.Token, cts.Token);
        }

        private readonly BackgroundProcessContext _context;
        private readonly FluentNHibernateJobStorage _storage;

        private static long CreateExpirationEntry(StatelessSessionWrapper session, DateTime? expireAt)
        {
            session.DeleteAll<_AggregatedCounter>();
            var a = new _AggregatedCounter { Key = "key", Value = 1, ExpireAt = expireAt };
            session.Insert(a);

            return a.Id;
        }

        private static bool IsEntryExpired(StatelessSessionWrapper session, long entryId)
        {
            var count = session.Query<_AggregatedCounter>().Count(i => i.Id == entryId);

            return count == 0;
        }

        private ExpirationManager CreateManager()
        {
            return new ExpirationManager(_storage);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null));
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            WithCleanTables(_storage, session =>
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(1));
                var manager = CreateManager();

                //Act
                manager.Execute(_context);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            });
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            WithCleanTables(_storage, session =>
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, null);
                var manager = CreateManager();

                //Act
                manager.Execute(_context);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            });
        }

        [Fact]
        public void Execute_Processes_AggregatedCounterTable()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                session.Insert(new _AggregatedCounter
                {
                    Key = "key",
                    Value = 1,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });

                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Counter>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_HashTable()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                session.Insert(new _Hash
                {
                    Key = "key1",
                    Field = "field",
                    Value = string.Empty,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });
                session.Insert(new _Hash
                {
                    Key = "key2",
                    Field = "field",
                    Value = string.Empty,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });
                //does nothing
                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Hash>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_JobTable()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                session.Insert(new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = session.Storage.UtcNow,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });


                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Job>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_ListTable()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                session.Insert(new _List { Key = "key", ExpireAt = session.Storage.UtcNow.AddMonths(-1) });


                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_List>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_SetTable()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                session.Insert(new _Set
                {
                    Key = "key",
                    Score = 0,
                    Value = string.Empty,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });


                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Set>().Count());
            });
        }

        [Fact]
        public void Execute_RemovesOutdatedRecords()
        {
            WithCleanTables(_storage, session =>
            {
                // Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(-1));
                var manager = CreateManager();
                // Act
                manager.Execute(_context);
                //Assert
                Assert.True(IsEntryExpired(session, entryId));
            });
        }
    }
}