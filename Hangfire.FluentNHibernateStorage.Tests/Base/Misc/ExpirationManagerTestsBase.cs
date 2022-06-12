using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Hangfire.Server;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class ExpirationManagerTestsBase : TestBase
    {
        protected ExpirationManagerTestsBase(DatabaseFixtureBase fixture) : base(fixture)
        {
        }

        public override FluentNHibernateJobStorage GetStorage(FluentNHibernateStorageOptions options = null)
        {
            var tmp = base.GetStorage(options);
            tmp.Options.JobExpirationCheckInterval = TimeSpan.Zero;
            return tmp;
        }

        private static long CreateExpirationEntry(StatelessSessionWrapper session, DateTime? expireAt)
        {
            session.DeleteAll<_AggregatedCounter>();
            var aggregatedCounter = new _AggregatedCounter {Key = "key", Value = 1, ExpireAt = expireAt};
            session.Insert(aggregatedCounter);

            return aggregatedCounter.Id;
        }

        private static bool IsEntryExpired(StatelessSessionWrapper session, long entryId)
        {
            var count = session.Query<_AggregatedCounter>().Count(i => i.Id == entryId);

            return count == 0;
        }

        private class TestInfo
        {
            public BackgroundProcessContext BackgroundProcessContext { get; set; }
            public ExpirationManager Manager { get; set; }
        }


        private TestInfo GetTestInfo(FluentNHibernateJobStorage storage)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var result = new TestInfo
            {
                Manager = new ExpirationManager(storage),
                BackgroundProcessContext = new BackgroundProcessContext("dummy", storage,
                    new Dictionary<string, object>(), Guid.NewGuid(),
                    cancellationTokenSource.Token, cancellationTokenSource.Token, cancellationTokenSource.Token)
            };

            return result;
        }


        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null));
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(1));
                var testInfo = GetTestInfo(session.Storage);

                //Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            });
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                var entryId = CreateExpirationEntry(session, null);
                var testInfo = GetTestInfo(session.Storage);

                //Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                //Assert
                Assert.False(IsEntryExpired(session, entryId));
            });
        }

        [Fact]
        public void Execute_Processes_AggregatedCounterTable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _AggregatedCounter
                {
                    Key = "key",
                    Value = 1,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });

                var testInfo = GetTestInfo(session.Storage);

                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                // Assert
                Assert.Equal(0, session.Query<_Counter>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_HashTable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
                var testInfo = GetTestInfo(session.Storage);

                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                // Assert
                Assert.Equal(0, session.Query<_Hash>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_JobTable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = session.Storage.UtcNow,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });


                var testInfo = GetTestInfo(session.Storage);

                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                // Assert
                Assert.Equal(0, session.Query<_Job>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_ListTable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _List {Key = "key", ExpireAt = session.Storage.UtcNow.AddMonths(-1)});


                var testInfo = GetTestInfo(session.Storage);

                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                // Assert
                Assert.Equal(0, session.Query<_List>().Count());
            });
        }

        [Fact]
        public void Execute_Processes_SetTable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _Set
                {
                    Key = "key",
                    Score = 0,
                    Value = string.Empty,
                    ExpireAt = session.Storage.UtcNow.AddMonths(-1)
                });


                var testInfo = GetTestInfo(session.Storage);

                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);

                // Assert
                Assert.Equal(0, session.Query<_Set>().Count());
            });
        }

        [Fact]
        public void Execute_RemovesOutdatedRecords()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                var entryId = CreateExpirationEntry(session, session.Storage.UtcNow.AddMonths(-1));
                var testInfo = GetTestInfo(session.Storage);
                // Act
                testInfo.Manager.Execute(testInfo.BackgroundProcessContext);
                //Assert
                Assert.True(IsEntryExpired(session, entryId));
            });
        }
    }
}