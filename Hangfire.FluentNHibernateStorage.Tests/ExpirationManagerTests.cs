using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests;
using Hangfire.Server;
using Xunit;

namespace Hangfire.FluentNHibernateJobStorage.Tests
{
    public class ExpirationManagerTests : IClassFixture<TestDatabaseFixture>
    {
        public ExpirationManagerTests()
        {
            _storage = ConnectionUtils.GetStorage();
            var cts = new CancellationTokenSource();
            _context = new BackgroundProcessContext("dummy", _storage, new Dictionary<string, object>(), cts.Token);
        }

        private readonly BackgroundProcessContext _context;
        private readonly FluentNHibernateStorage.FluentNHibernateJobStorage _storage;

        private static int CreateExpirationEntry(IWrappedSession session, DateTime? expireAt)
        {
            session.DeleteAll<_AggregatedCounter>();
            var a = new _AggregatedCounter {Key = "key", Value = 1, ExpireAt = expireAt};
            session.Insert(a);
            session.Flush();
            return a.Id;
        }

        private static bool IsEntryExpired(IWrappedSession session, int entryId)
        {
            var count = session.Query<_AggregatedCounter>().Count(i => i.Id == entryId);

            return count == 0;
        }

        private ExpirationManager CreateManager()
        {
            return new ExpirationManager(_storage, TimeSpan.Zero);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null));
        }

        [Fact]
        [CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            using (var session = _storage.GetStatefulSession())
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(1));
                var manager = CreateManager();

                //Act
                manager.Execute(_context);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            using (var session = _storage.GetStatefulSession())
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, null);
                var manager = CreateManager();

                //Act
                manager.Execute(_context);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_Processes_AggregatedCounterTable()
        {
            using (var session = _storage.GetStatefulSession())
            {
                // Arrange
                session.Insert(new _AggregatedCounter
                {
                    Key = "key",
                    Value = 1,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });
                session.Flush();

                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Counter>().Count());
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_Processes_HashTable()
        {
            using (var session = _storage.GetStatefulSession())
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
                session.Flush();
                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Hash>().Count());
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_Processes_JobTable()
        {
            using (var session = _storage.GetStatefulSession())
            {
                // Arrange
                session.Insert(new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = session.Storage.UtcNow,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });
                session.Flush();


                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Job>().Count());
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_Processes_ListTable()
        {
            using (var session = _storage.GetStatefulSession())
            {
                // Arrange
                session.Insert(new _List {Key = "key", ExpireAt = session.Storage.UtcNow.AddMonths(-1)});
                session.Flush();

                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_List>().Count());
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_Processes_SetTable()
        {
            using (var session = _storage.GetStatefulSession())
            {
                // Arrange
                session.Insert(new _Set
                {
                    Key = "key",
                    Score = 0,
                    Value = string.Empty,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });
                session.Flush();

                var manager = CreateManager();

                // Act
                manager.Execute(_context);

                // Assert
                Assert.Equal(0, session.Query<_Set>().Count());
            }
        }

        [Fact]
        [CleanDatabase]
        public void Execute_RemovesOutdatedRecords()
        {
            using (var session = _storage.GetStatefulSession())
            {
                // Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(-1));
                var manager = CreateManager();
                // Act
                manager.Execute(_context);
                //Assert
                Assert.True(IsEntryExpired(session, entryId));
            }
        }
    }
}