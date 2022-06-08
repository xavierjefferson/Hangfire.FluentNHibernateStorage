using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Providers;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class FluentNHibernateStorageConnectionTestsBase<T, U> : TestBase<T, U> where T : IDbProvider, new() where U : TestDatabaseFixture
    {
        public FluentNHibernateStorageConnectionTestsBase()
        {
            _queue = new Mock<IPersistentJobQueue>();

            var provider = new Mock<IPersistentJobQueueProvider>();
            provider.Setup(x => x.GetJobQueue())
                .Returns(_queue.Object);

            _providers = new PersistentJobQueueProviderCollection(provider.Object);
        }

        private readonly PersistentJobQueueProviderCollection _providers;
        private readonly Mock<IPersistentJobQueue> _queue;

        private void UseJobStorageConnectionWithSession(
            Action<StatelessSessionWrapper, FluentNHibernateJobStorageConnection> action)
        {
            UseJobStorageConnection(jobsto => { jobsto.Storage.UseStatelessSession(s => action(s, jobsto)); });
        }

        private void UseJobStorageConnection(Action<FluentNHibernateJobStorageConnection> action)
        {
            var storage = GetMockStorage();

            using (var jobStorage = new FluentNHibernateJobStorageConnection(storage.Object))
            {
                action(jobStorage);
            }
        }

      
        private Mock<FluentNHibernateJobStorage> GetMockStorage()
        {
            var persistenceConfigurer = GetPersistenceConfigurer();
            var storage = CreateMock(persistenceConfigurer);
            storage.Setup(x => x.QueueProviders).Returns(_providers);
            return storage;
        }

        public static void SampleMethod(string arg)
        {
        }

        [Fact]
        public void AcquireLock_ReturnsNonNullInstance()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var @lock = jobStorage.AcquireDistributedLock("1", TimeSpan.FromSeconds(1));
                Assert.NotNull(@lock);
            });
        }

        [Fact]
        public void AnnounceServer_CreatesOrUpdatesARecord()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var queues = new[] {"critical", "default"};
                var context1 = new ServerContext
                {
                    Queues = queues,
                    WorkerCount = 4
                };
                jobStorage.AnnounceServer("server", context1);

                var server = session.Query<_Server>().Single();
                Assert.Equal("server", server.Id);
                Assert.NotNull(server.Data);
                var serverData1 = SerializationHelper.Deserialize<ServerData>(server.Data);
                Assert.Equal(4, serverData1.WorkerCount);
                Assert.Equal(queues, serverData1.Queues);
                Assert.NotNull(server.LastHeartbeat);

                var context2 = new ServerContext
                {
                    Queues = new[] {"default"},
                    WorkerCount = 1000
                };
                jobStorage.AnnounceServer("server", context2);

                var sameServer = session.Query<_Server>().Single();
                Assert.Equal("server", sameServer.Id);
                Assert.NotNull(sameServer.Data);
                var serverData2 = SerializationHelper.Deserialize<ServerData>(sameServer.Data);
                Assert.Equal(1000, serverData2.WorkerCount);
            });
        }

        [Fact]
        public void AnnounceServer_ThrowsAnException_WhenContextIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.AnnounceServer("server", null));

                Assert.Equal("context", exception.ParamName);
            });
        }

        [Fact]
        public void AnnounceServer_ThrowsAnException_WhenServerIdIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.AnnounceServer(null, new ServerContext()));

                Assert.Equal("serverId", exception.ParamName);
            });
        }

        [Fact]
        public void CreateExpiredJob_CreatesAJobInTheStorage_AndSetsItsParameters()
        {
            const string expectedString = "Hello";
            const string key1 = "Key1";
            const string key2 = "Key2";
            const string expectedValue1 = "Value1";
            const string expectedValue2 = "Value2";
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var createdAt = new DateTime(2012, 12, 12).ToUniversalTime();
                var dictionary = new Dictionary<string, string> {{key1, expectedValue1}, {key2, expectedValue2}};
                var jobId = jobStorage.CreateExpiredJob(
                    Job.FromExpression(() => SampleMethod(expectedString)),
                    dictionary,
                    createdAt,
                    TimeSpan.FromDays(1));

                Assert.NotNull(jobId);
                Assert.NotEmpty(jobId);

                var sqlJob = session.Query<_Job>().Single();
                Assert.Equal(jobId, sqlJob.Id.ToString());
                Assert.Equal(createdAt, sqlJob.CreatedAt);
                Assert.Null(sqlJob.StateName);

                var invocationData = SerializationHelper.Deserialize<InvocationData>(sqlJob.InvocationData);
                invocationData.Arguments = sqlJob.Arguments;

                var job = invocationData.DeserializeJob();
                Assert.Equal(typeof(FluentNHibernateStorageConnectionTestsBase<T,U>), job.Type);
                Assert.Equal(nameof(SampleMethod), job.Method.Name);
                Assert.Equal(expectedString, job.Args[0]);

                Assert.True(createdAt.AddDays(1).AddMinutes(-1) < sqlJob.ExpireAt);
                Assert.True(sqlJob.ExpireAt < createdAt.AddDays(1).AddMinutes(1));

                var parameters = session.Query<_JobParameter>()
                    .Where(i => i.Job.Id == long.Parse(jobId))
                    .ToDictionary(x => x.Name, x => x.Value);

                Assert.Equal(expectedValue1, parameters[key1]);
                Assert.Equal(expectedValue2, parameters[key2]);
            });
        }

        [Fact]
        public void CreateExpiredJob_ThrowsAnException_WhenJobIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.CreateExpiredJob(
                        null,
                        new Dictionary<string, string>(),
                        jobStorage.Storage.UtcNow,
                        TimeSpan.Zero));

                Assert.Equal("job", exception.ParamName);
            });
        }

        [Fact]
        public void CreateExpiredJob_ThrowsAnException_WhenParametersCollectionIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.CreateExpiredJob(
                        Job.FromExpression(() => SampleMethod("hello")),
                        null,
                        jobStorage.Storage.UtcNow,
                        TimeSpan.Zero));

                Assert.Equal("parameters", exception.ParamName);
            });
        }

        [Fact]
        public void CreateWriteTransaction_ReturnsNonNullInstance()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var transaction = jobStorage.CreateWriteTransaction();
                Assert.NotNull(transaction);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateJobStorageConnection(null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void FetchNextJob_DelegatesItsExecution_ToTheQueue()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var token = new CancellationToken();
                var queues = new[] {"default"};

                jobStorage.FetchNextJob(queues, token);

                _queue.Verify(x => x.Dequeue(queues, token));
            });
        }

        [Fact]
        public void FetchNextJob_Throws_IfMultipleProvidersResolved()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var token = new CancellationToken();
                var anotherProvider = new Mock<IPersistentJobQueueProvider>();
                _providers.Add(anotherProvider.Object, new[] {"critical"});

                Assert.Throws<InvalidOperationException>(
                    () => jobStorage.FetchNextJob(new[] {"critical", "default"}, token));
            });
        }

        [Fact]
        public void GetAllEntriesFromHash_ReturnsAllKeysAndTheirValues()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                var hashes = new[]
                {
                    new _Hash {Key = "some-hash", Field = "Key1", Value = "Value1"},
                    new _Hash {Key = "some-hash", Field = "Key2", Value = "Value2"},
                    new _Hash {Key = "another-hash", Field = "Key3", Value = "Value3"}
                };
                foreach (var hash in hashes) session.Insert(hash);

                //does nothing
                // Act

                var result = jobStorage.GetAllEntriesFromHash("some-hash");

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Fact]
        public void GetAllEntriesFromHash_ReturnsNull_IfHashDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetAllEntriesFromHash("some-hash");
                Assert.Null(result);
            });
        }

        [Fact]
        public void GetAllEntriesFromHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
                Assert.Throws<ArgumentNullException>(() => jobStorage.GetAllEntriesFromHash(null)));
        }

        [Fact]
        public void GetAllItemsFromList_ReturnsAllItems_FromAGivenList()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _List {Key = "list-1", Value = "1"},
                    new _List {Key = "list-2", Value = "2"},
                    new _List {Key = "list-1", Value = "3"}
                });
                //does nothing
                // Act

                var result = jobStorage.GetAllItemsFromList("list-1");

                // Assert
                Assert.Equal(new[] {"3", "1"}, result.ToArray());
            });
        }

        [Fact]
        public void GetAllItemsFromList_ReturnsAnEmptyList_WhenListDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetAllItemsFromList("my-list");
                Assert.Empty(result);
            });
        }

        [Fact]
        public void GetAllItemsFromList_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetAllItemsFromList(null));
            });
        }

        [Fact]
        public void GetAllItemsFromSet_ReturnsAllItems()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                var sets = new[]
                {
                    new _Set {Key = "some-set", Value = "1"},
                    new _Set {Key = "some-set", Value = "2"},
                    new _Set {Key = "another-set", Value = "3"}
                };
                foreach (var set in sets)
                    session.Insert(set);
                //does nothing
                // Act

                var result = jobStorage.GetAllItemsFromSet("some-set");

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Contains("1", result);
                Assert.Contains("2", result);
            });
        }

        [Fact]
        public void GetAllItemsFromSet_ReturnsEmptyCollection_WhenKeyDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetAllItemsFromSet("some-set");

                Assert.NotNull(result);
                Assert.Empty(result);
            });
        }

        [Fact]
        public void GetAllItemsFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
                Assert.Throws<ArgumentNullException>(() => jobStorage.GetAllItemsFromSet(null)));
        }

        [Fact]
        public void GetCounter_IncludesValues_FromCounterAggregateTable()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                var aggregatedCounters = new[]
                {
                    new _AggregatedCounter {Key = "counter-1", Value = 12},
                    new _AggregatedCounter {Key = "counter-2", Value = 15}
                };
                foreach (var aggregatedCounter in aggregatedCounters)
                    session.Insert(aggregatedCounter);
                ;
                //does nothing
                // Act

                var result = jobStorage.GetCounter("counter-1");

                Assert.Equal(12, result);
            });
        }

        [Fact]
        public void GetCounter_ReturnsSumOfValues_InCounterTable()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _Counter {Key = "counter-1", Value = 1},
                    new _Counter {Key = "counter-2", Value = 1},
                    new _Counter {Key = "counter-1", Value = 1}
                });
                //does nothing
                // Act

                var result = jobStorage.GetCounter("counter-1");

                // Assert
                Assert.Equal(2, result);
            });
        }

        [Fact]
        public void GetCounter_ReturnsZero_WhenKeyDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetCounter("my-counter");
                Assert.Equal(0, result);
            });
        }


        [Fact]
        public void GetCounter_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetCounter(null));
            });
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ReturnsNull_WhenTheKeyDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetFirstByLowestScoreFromSet(
                    "Key", 0, 1);

                Assert.Null(result);
            });
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ReturnsTheValueWithTheLowestScore()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(new _Set {Key = "Key", Score = 1, Value = "1.0"});
                session.Insert(new _Set {Key = "Key", Score = -1, Value = "-1.0"});
                session.Insert(new _Set {Key = "Key", Score = -5, Value = "-5.0"});
                session.Insert(new _Set {Key = "another-Key", Score = -2, Value = "-2.0"});
                //does nothing

                var result = jobStorage.GetFirstByLowestScoreFromSet("Key", -1.0, 3.0);

                Assert.Equal("-1.0", result);
            });
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_ToScoreIsLowerThanFromScore()
        {
            UseJobStorageConnection(jobStorage => Assert.Throws<ArgumentException>(
                () => jobStorage.GetFirstByLowestScoreFromSet("Key", 0, -1)));
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetFirstByLowestScoreFromSet(null, 0, 1));

                Assert.Equal("Key", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        [Fact]
        public void GetHashCount_ReturnsNumber_OfHashFields()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _Hash {Key = "hash-1", Field = "Field-1"},
                    new _Hash {Key = "hash-1", Field = "Field-2"},
                    new _Hash {Key = "hash-2", Field = "Field-1"}
                });
                //does nothing
                // Act

                var result = jobStorage.GetHashCount("hash-1");

                // Assert
                Assert.Equal(2, result);
            });
        }

        [Fact]
        public void GetHashCount_ReturnsZero_WhenKeyDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetHashCount("my-hash");
                Assert.Equal(0, result);
            });
        }

        [Fact]
        public void GetHashCount_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(() => jobStorage.GetHashCount(null));
            });
        }

        [Fact]
        public void GetHashTtl_ReturnsExpirationTimeForHash()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _Hash {Key = "hash-1", Field = "Field", ExpireAt = session.Storage.UtcNow.AddHours(1)},
                    new _Hash {Key = "hash-2", Field = "Field", ExpireAt = null}
                });
                //does nothing
                // Act

                var result = jobStorage.GetHashTtl("hash-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            });
        }

        [Fact]
        public void GetHashTtl_ReturnsNegativeValue_WhenHashDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetHashTtl("my-hash");
                Assert.True(result < TimeSpan.Zero);
            });
        }

        [Fact]
        public void GetHashTtl_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetHashTtl(null));
            });
        }

        [Fact]
        public void GetJobData_ReturnsJobLoadException_IfThereWasADeserializationException()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var newJob = new _Job
                {
                    InvocationData = SerializationHelper.Serialize(new InvocationData(null, null, null, null)),
                    StateName = "Succeeded",
                    Arguments = "['Arguments']",
                    CreatedAt = session.Storage.UtcNow
                };
                session.Insert(newJob);
                //does nothing

                var result = jobStorage.GetJobData(newJob.Id.ToString());

                Assert.NotNull(result.LoadException);
            });
        }

        [Fact]
        public void GetJobData_ReturnsNull_WhenThereIsNoSuchJob()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetJobData("1");
                Assert.Null(result);
            });
        }

        [Fact]
        public void GetJobData_ReturnsResult_WhenJobExists()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var job = Job.FromExpression(() => SampleMethod("wrong"));
                var newJob = new _Job
                {
                    InvocationData = SerializationHelper.Serialize(InvocationData.SerializeJob(job)),
                    StateName = "Succeeded",
                    Arguments = "['Arguments']", CreatedAt = session.Storage.UtcNow
                };
                session.Insert(newJob);
                //does nothing
                var jobId = newJob.Id;


                var result = jobStorage.GetJobData(jobId.ToString());

                Assert.NotNull(result);
                Assert.NotNull(result.Job);
                Assert.Equal("Succeeded", result.State);
                Assert.Equal("Arguments", result.Job.Args[0]);
                Assert.Null(result.LoadException);
                Assert.True(session.Storage.UtcNow.AddMinutes(-1) < result.CreatedAt.ToUniversalTime());
                Assert.True(result.CreatedAt.ToUniversalTime() < session.Storage.UtcNow.AddMinutes(1));
            });
        }

        [Fact]
        public void GetJobData_ThrowsAnException_WhenJobIdIsNull()
        {
            UseJobStorageConnection(jobStorage => Assert.Throws<ArgumentNullException>(
                () => jobStorage.GetJobData(null)));
        }

        [Fact]
        public void GetListCount_ReturnsTheNumberOfListElements()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _List {Key = "list-1"},
                    new _List {Key = "list-1"},
                    new _List {Key = "list-2"}
                });
                //does nothing
                // Act

                var result = jobStorage.GetListCount("list-1");

                // Assert
                Assert.Equal(2, result);
            });
        }

        [Fact]
        public void GetListCount_ReturnsZero_WhenListDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetListCount("my-list");
                Assert.Equal(0, result);
            });
        }

        [Fact]
        public void GetListCount_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetListCount(null));
            });
        }

        [Fact]
        public void GetListTtl_ReturnsExpirationTimeForList()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _List {Key = "list-1", ExpireAt = session.Storage.UtcNow.AddHours(1)},
                    new _List {Key = "list-2", ExpireAt = null}
                });
                //does nothing
                // Act

                var result = jobStorage.GetListTtl("list-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            });
        }

        [Fact]
        public void GetListTtl_ReturnsNegativeValue_WhenListDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetListTtl("my-list");
                Assert.True(result < TimeSpan.Zero);
            });
        }

        [Fact]
        public void GetListTtl_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetListTtl(null));
            });
        }

        [Fact]
        public void GetParameter_ReturnsNull_WhenParameterDoesNotExists()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var Value = jobStorage.GetJobParameter("1", "hello");
                Assert.Null(Value);
            });
        }

        [Fact]
        public void GetParameter_ReturnsParameterValue_WhenJobExists()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                session.Insert(new _JobParameter {Job = newJob, Name = "name", Value = "Value"});
                //does nothing


                var Value = jobStorage.GetJobParameter(newJob.Id.ToString(), "name");

                Assert.Equal("Value", Value);
            });
        }

        [Fact]
        public void GetParameter_ThrowsAnException_WhenJobIdIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetJobParameter(null, "hello"));

                Assert.Equal("id", exception.ParamName);
            });
        }

        [Fact]
        public void GetParameter_ThrowsAnException_WhenNameIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetJobParameter("1", null));

                Assert.Equal("name", exception.ParamName);
            });
        }

        [Fact]
        public void GetRangeFromList_ReturnsAllEntries_WithinGivenBounds()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _List {Key = "list-1", Value = "1"},
                    new _List {Key = "list-2", Value = "2"},
                    new _List {Key = "list-1", Value = "3"},
                    new _List {Key = "list-1", Value = "4"},
                    new _List {Key = "list-1", Value = "5"}
                });
                //does nothing
                // Act

                var result = jobStorage.GetRangeFromList("list-1", 1, 2);

                // Assert
                Assert.Equal(new[] {"4", "3"}, result);
            });
        }

        [Fact]
        public void GetRangeFromList_ReturnsAnEmptyList_WhenListDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetRangeFromList("my-list", 0, 1);
                Assert.Empty(result);
            });
        }

        [Fact]
        public void GetRangeFromList_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetRangeFromList(null, 0, 1));

                Assert.Equal("Key", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        [Fact]
        public void GetRangeFromSet_ReturnsPagedElements()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(new List<dynamic>
                {
                    new _Set {Key = "set-1", Value = "1"},
                    new _Set {Key = "set-1", Value = "2"},
                    new _Set {Key = "set-1", Value = "3"},
                    new _Set {Key = "set-1", Value = "4"},
                    new _Set {Key = "set-2", Value = "4"},
                    new _Set {Key = "set-1", Value = "5"}
                });
                //does nothing

                var result = jobStorage.GetRangeFromSet("set-1", 2, 3);

                Assert.Equal(new[] {"3", "4"}, result);
            });
        }

        [Fact]
        public void GetRangeFromSet_ReturnsPagedElements2()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(new List<_Set>
                {
                    new _Set {Key = "set-1", Value = "1"},
                    new _Set {Key = "set-1", Value = "2"},
                    new _Set {Key = "set-0", Value = "3"},
                    new _Set {Key = "set-1", Value = "4"},
                    new _Set {Key = "set-2", Value = "1"},
                    new _Set {Key = "set-1", Value = "5"},
                    new _Set {Key = "set-2", Value = "2"},
                    new _Set {Key = "set-1", Value = "3"}
                });
                //does nothing

                var result = jobStorage.GetRangeFromSet("set-1", 0, 4);

                Assert.Equal(new[] {"1", "2", "4", "5", "3"}, result);
            });
        }

        [Fact]
        public void GetRangeFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(() => jobStorage.GetRangeFromSet(null, 0, 1));
            });
        }

        [Fact]
        public void GetSetCount_ReturnsNumberOfElements_InASet()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(new List<_Set>
                {
                    new _Set {Key = "set-1", Value = "Value-1"},
                    new _Set {Key = "set-2", Value = "Value-1"},
                    new _Set {Key = "set-1", Value = "Value-2"}
                });
                //does nothing

                var result = jobStorage.GetSetCount("set-1");

                Assert.Equal(2, result);
            });
        }

        [Fact]
        public void GetSetCount_ReturnsZero_WhenSetDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetSetCount("my-set");
                Assert.Equal(0, result);
            });
        }

        [Fact]
        public void GetSetCount_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetSetCount(null));
            });
        }

        [Fact]
        public void GetSetTtl_ReturnsExpirationTime_OfAGivenSet()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _Set {Key = "set-1", Value = "1", ExpireAt = session.Storage.UtcNow.AddMinutes(60)},
                    new _Set {Key = "set-2", Value = "2", ExpireAt = null}
                });
                //does nothing

                // Act
                var result = jobStorage.GetSetTtl("set-1");

                // Assert
                Assert.True(TimeSpan.FromMinutes(59) < result);
                Assert.True(result < TimeSpan.FromMinutes(61));
            });
        }

        [Fact]
        public void GetSetTtl_ReturnsNegativeValue_WhenSetDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetSetTtl("my-set");
                Assert.True(result < TimeSpan.Zero);
            });
        }

        [Fact]
        public void GetSetTtl_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                Assert.Throws<ArgumentNullException>(() => jobStorage.GetSetTtl(null));
            });
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var data = new Dictionary<string, string>
                {
                    {"Key", "Value"}
                };
                var newJob = new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    StateName = string.Empty,
                    CreatedAt = session.Storage.UtcNow
                };
                session.Insert(newJob);
                session.Insert(new _JobState {Job = newJob, Name = "old-state", CreatedAt = session.Storage.UtcNow});
                var lastState = new _JobState
                {
                    Job = newJob,
                    Name = "Name",
                    Reason = "Reason",
                    CreatedAt = session.Storage.UtcNow,
                    Data = SerializationHelper.Serialize(data)
                };
                session.Insert(lastState);
                //does nothing
                newJob.StateName = lastState.Name;
                newJob.StateReason = lastState.Reason;
                newJob.StateData = lastState.Data;
                newJob.LastStateChangedAt = session.Storage.UtcNow;
                session.Update(newJob);
                //does nothing


                var jobId = newJob.Id;

                var result = jobStorage.GetStateData(jobId.ToString());
                Assert.NotNull(result);

                Assert.Equal("Name", result.Name);
                Assert.Equal("Reason", result.Reason);
                Assert.Equal("Value", result.Data["Key"]);
            });
        }

        [Fact]
        public void GetStateData_ReturnsCorrectData_WhenPropertiesAreCamelcased()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var data = new Dictionary<string, string>
                {
                    {"Key", "Value"}
                };
                var newJob = JobInsertionHelper.InsertNewJob(session);
                session.Insert(new _JobState {Job = newJob, Name = "old-state", CreatedAt = session.Storage.UtcNow});
                var jobState = new _JobState
                {
                    Job = newJob,
                    Name = "Name",
                    Reason = "Reason",
                    CreatedAt = session.Storage.UtcNow,
                    Data = SerializationHelper.Serialize(data)
                };
                session.Insert(jobState);
                //does nothing
                newJob.StateName = jobState.Name;
                newJob.StateReason = jobState.Reason;
                newJob.StateData = jobState.Data;
                session.Update(newJob);
                //does nothing


                var jobId = newJob.Id;

                var result = jobStorage.GetStateData(jobId.ToString());
                Assert.NotNull(result);

                Assert.Equal("Value", result.Data["Key"]);
            });
        }

        [Fact]
        public void GetStateData_ReturnsNull_IfThereIsNoSuchState()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetStateData("1");
                Assert.Null(result);
            });
        }

        [Fact]
        public void GetStateData_ThrowsAnException_WhenJobIdIsNull()
        {
            UseJobStorageConnection(
                jobStorage => Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetStateData(null)));
        }

        [Fact]
        public void GetValueFromHash_ReturnsNull_WhenHashDoesNotExist()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var result = jobStorage.GetValueFromHash("my-hash", "name");
                Assert.Null(result);
            });
        }

        [Fact]
        public void GetValueFromHash_ReturnsValue_OfAGivenField()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                // Arrange
                session.Insert(new[]
                {
                    new _Hash {Key = "hash-1", Field = "Field-1", Value = "1"},
                    new _Hash {Key = "hash-1", Field = "Field-2", Value = "2"},
                    new _Hash {Key = "hash-2", Field = "Field-1", Value = "3"}
                });
                //does nothing

                // Act
                var result = jobStorage.GetValueFromHash("hash-1", "Field-1");

                // Assert
                Assert.Equal("1", result);
            });
        }

        [Fact]
        public void GetValueFromHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetValueFromHash(null, "name"));

                Assert.Equal("Key", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        [Fact]
        public void GetValueFromHash_ThrowsAnException_WhenNameIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.GetValueFromHash("Key", null));

                Assert.Equal("name", exception.ParamName);
            });
        }

        [Fact]
        public void Heartbeat_ThrowsAnException_WhenServerIdIsNull()
        {
            UseJobStorageConnection(jobStorage => Assert.Throws<ArgumentNullException>(
                () => jobStorage.Heartbeat(null)));
        }

        [Fact]
        public void Heartbeat_UpdatesLastHeartbeat_OfTheServerWithGivenId()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(new _Server
                {
                    Id = "server1",
                    Data = string.Empty,
                    LastHeartbeat = new DateTime(2012, 12, 12, 12, 12, 12)
                });
                session.Insert(new _Server
                {
                    Id = "server2",
                    Data = string.Empty,
                    LastHeartbeat = new DateTime(2012, 12, 12, 12, 12, 12)
                });
                //does nothing

                jobStorage.Heartbeat("server1");

                var servers = session.Query<_Server>()
                    .ToDictionary(x => x.Id, x => x.LastHeartbeat.Value);

                Assert.NotEqual(2012, servers["server1"].Year);
                Assert.Equal(2012, servers["server2"].Year);
            });
        }

        [Fact]
        public void RemoveServer_RemovesAServerRecord()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(
                    new _Server {Id = "Server1", Data = string.Empty, LastHeartbeat = session.Storage.UtcNow});
                session.Insert(
                    new _Server {Id = "Server2", Data = string.Empty, LastHeartbeat = session.Storage.UtcNow});
                //does nothing

                jobStorage.RemoveServer("Server1");

                var server = session.Query<_Server>().Single();
                Assert.NotEqual("Server1", server.Id, StringComparer.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public void RemoveServer_ThrowsAnException_WhenServerIdIsNull()
        {
            UseJobStorageConnection(jobStorage => Assert.Throws<ArgumentNullException>(
                () => jobStorage.RemoveServer(null)));
        }

        [Fact]
        public void RemoveTimedOutServers_DoItsWorkPerfectly()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                session.Insert(
                    new[]
                    {
                        new _Server
                        {
                            Id = "server1", LastHeartbeat = session.Storage.UtcNow.AddDays(-1), Data = string.Empty
                        },
                        new _Server
                        {
                            Id = "server2", LastHeartbeat = session.Storage.UtcNow.AddHours(-12), Data = string.Empty
                        }
                    });
                //does nothing
                jobStorage.RemoveTimedOutServers(TimeSpan.FromHours(15));

                var liveServer = session.Query<_Server>().Single();
                Assert.Equal("server2", liveServer.Id);
            });
        }

        [Fact]
        public void RemoveTimedOutServers_ThrowsAnException_WhenTimeOutIsNegative()
        {
            UseJobStorageConnection(jobStorage => Assert.Throws<ArgumentException>(
                () => jobStorage.RemoveTimedOutServers(TimeSpan.FromMinutes(-5))));
        }

        [Fact]
        public void SetParameter_CanAcceptNulls_AsValues()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                var jobId = newJob.Id.ToString();
                //does nothing

                jobStorage.SetJobParameter(jobId, "Name", null);

                var parameter = session.Query<_JobParameter>().Single(i => i.Job == newJob && i.Name == "Name");

                Assert.Null(parameter.Value);
            });
        }

        [Fact]
        public void SetParameter_ThrowsAnException_WhenJobIdIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.SetJobParameter(null, "name", "Value"));

                Assert.Equal("id", exception.ParamName);
            });
        }

        [Fact]
        public void SetParameter_ThrowsAnException_WhenNameIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.SetJobParameter("1", null, "Value"));

                Assert.Equal("name", exception.ParamName);
            });
        }

        [Fact]
        public void SetParameter_UpdatesValue_WhenParameterWithTheGivenName_AlreadyExists()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);
                var jobId = newJob.Id.ToString();

                jobStorage.SetJobParameter(jobId, "Name", "Value");
                jobStorage.SetJobParameter(jobId, "Name", "AnotherValue");
                //does nothing

                var parameter = session.Query<_JobParameter>().Single(i => i.Job == newJob && i.Name == "Name");

                Assert.Equal("AnotherValue", parameter.Value);
            });
        }

        [Fact]
        public void SetParameters_CreatesNewParameter_WhenParameterWithTheGivenNameDoesNotExists()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                var newJob = JobInsertionHelper.InsertNewJob(session);

                var jobId = newJob.Id.ToString();

                jobStorage.SetJobParameter(jobId, "Name", "Value");
                //does nothing

                var parameter = session.Query<_JobParameter>().Single(i => i.Job == newJob && i.Name == "Name");

                Assert.Equal("Value", parameter.Value);
            });
        }

        [Fact]
        public void SetRangeInHash_MergesAllRecords()
        {
            UseJobStorageConnectionWithSession((session, jobStorage) =>
            {
                jobStorage.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                });
                //does nothing

                var result = session.Query<_Hash>().Where(i => i.Key == "some-hash")
                    .ToDictionary(x => x.Field, x => x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Fact]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.SetRangeInHash(null, new Dictionary<string, string>()));

                Assert.Equal("Key", exception.ParamName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        [Fact]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull()
        {
            UseJobStorageConnection(jobStorage =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => jobStorage.SetRangeInHash("some-hash", null));

                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }
    }
}