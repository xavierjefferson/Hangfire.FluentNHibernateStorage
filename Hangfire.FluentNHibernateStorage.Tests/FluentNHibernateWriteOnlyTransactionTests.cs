using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class FluentNHibernateWriteOnlyTransactionTests : IClassFixture<TestDatabaseFixture>
    {
        public FluentNHibernateWriteOnlyTransactionTests()
        {
            var defaultProvider = new Mock<IPersistentJobQueueProvider>();
            defaultProvider.Setup(x => x.GetJobQueue())
                .Returns(new Mock<IPersistentJobQueue>().Object);

            _queueProviders = new PersistentJobQueueProviderCollection(defaultProvider.Object);
        }

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        private class InsertTwoJobsResult
        {
            public string JobId1 { get; set; }
            public string JobId2 { get; set; }
        }

        private static InsertTwoJobsResult InsertTwoJobs(IWrappedSession session)
        {
            var insertTwoJobsResult = new InsertTwoJobsResult();


            for (var i = 0; i < 2; i++)
            {
                var newJob = new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = session.Storage.UtcNow
                };
                session.Insert(newJob);
                session.Flush();

                if (i == 0)
                {
                    insertTwoJobsResult.JobId1 = newJob.Id.ToString();
                }
                else
                {
                    insertTwoJobsResult.JobId2 = newJob.Id.ToString();
                }
            }
            return insertTwoJobsResult;
        }

        private static dynamic GetTestJob(IWrappedSession connection, string jobId)
        {
            return connection.Query<_Job>().Single(i => i.Id == int.Parse(jobId));
        }

        private static void UseSession(Action<IWrappedSession> action)
        {
            using (var storage = ConnectionUtils.CreateStorage())
            {
                action(storage.GetStatefulSession());
            }
        }

        private void Commit(
            IWrappedSession connection,
            Action<FluentNHibernateWriteOnlyTransaction> action)
        {
            var storage = new Mock<FluentNHibernateJobStorage>(connection);
            storage.Setup(x => x.QueueProviders).Returns(_queueProviders);

            using (var transaction = new FluentNHibernateWriteOnlyTransaction(storage.Object))
            {
                action(transaction);
                transaction.Commit();
            }
        }

        [Fact]
        [CleanDatabase]
        public void AddJobState_JustAddsANewRecordInATable()
        {
            UseSession(session =>
            {
                //Arrange
                var newJob = new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = session.Storage.UtcNow
                };
                session.Insert(newJob);
                session.Flush();

                var jobId = newJob.Id;

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> {{"Name", "Value"}});

                Commit(session, x => x.AddJobState(jobId.ToString(), state.Object));

                var job = GetTestJob(session, jobId.ToString());
                Assert.Null(job.StateName);
                Assert.Null(job.StateId);

                var jobState = session.Query<_JobState>().Single();
                Assert.Equal(jobId, jobState.Job.Id);
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddRangeToSet_AddsAllItems_ToAGivenSet()
        {
            UseSession(session =>
            {
                var items = new List<string> {"1", "2", "3"};

                Commit(session, x => x.AddRangeToSet("my-set", items));

                var records = session.Query<_Set>().Where(i => i.Key == "my-set").Select(i => i.Value).ToList();
                Assert.Equal(items, records);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddRangeToSet_ThrowsAnException_WhenItemsValueIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.AddRangeToSet("my-set", null)));

                Assert.Equal("items", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddRangeToSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.AddRangeToSet(null, new List<string>())));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToQueue_CallsEnqueue_OnTargetPersistentQueue()
        {
            var correctJobQueue = new Mock<IPersistentJobQueue>();
            var correctProvider = new Mock<IPersistentJobQueueProvider>();
            correctProvider.Setup(x => x.GetJobQueue())
                .Returns(correctJobQueue.Object);

            _queueProviders.Add(correctProvider.Object, new[] {"default"});

            UseSession(session =>
            {
                Commit(session, x => x.AddToQueue("default", "1"));

                correctJobQueue.Verify(x => x.Enqueue(It.IsNotNull<IWrappedSession>(), "default", "1"));
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToSet_AddsARecord_IfThereIsNo_SuchKeyAndValue()
        {
            UseSession(session =>
            {
                Commit(session, x => x.AddToSet("my-key", "my-value"));

                var record = session.Query<_Set>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(0.0, record.Score, 2);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToSet_AddsARecord_WhenKeyIsExists_ButValuesAreDifferent()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "another-value");
                });

                var recordCount = session.Query<_Set>().Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToSet_DoesNotAddARecord_WhenBothKeyAndValueAreExist()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value");
                });

                var recordCount = session.Query<_Set>().Count();
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToSet_WithScore_AddsARecordWithScore_WhenBothKeyAndValueAreNotExist()
        {
            UseSession(session =>
            {
                Commit(session, x => x.AddToSet("my-key", "my-value", 3.2));

                var record = session.Query<_Set>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(3.2, record.Score, 3);
            });
        }

        [Fact]
        [CleanDatabase]
        public void AddToSet_WithScore_UpdatesAScore_WhenBothKeyAndValueAreExist()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value", 3.2);
                });

                var record = session.Query<_Set>().Single();

                Assert.Equal(3.2, record.Score, 3);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_IfStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FluentNHibernateWriteOnlyTransaction(null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        [CleanDatabase]
        public void DecrementCounter_AddsRecordToCounterTable_WithNegativeValue()
        {
            UseSession(session =>
            {
                Commit(session, x => x.DecrementCounter("my-key"));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.Equal(null, record.ExpireAt);
            });
        }

        [Fact]
        [CleanDatabase]
        public void DecrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.DecrementCounter("my-key");
                    x.DecrementCounter("my-key");
                });


                var recordCount = session.Query<_Counter>().Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void DecrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseSession(session =>
            {
                Commit(session, x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireHash_SetsExpirationTimeOnAHash_WithGivenKey()
        {
            UseSession(session =>
            {
                // Arrange
                session.Insert(new _Hash {Key = "hash-1", Field = "field"});
                session.Insert(new _Hash {Key = "hash-2", Field = "field"});
                session.Flush();

                // Act
                Commit(session, x => x.ExpireHash("hash-1", TimeSpan.FromMinutes(60)));

                // Assert
                var records = session.Query<_Hash>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["hash-1"]);
                Assert.True(records["hash-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["hash-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireHash(null, TimeSpan.FromMinutes(5))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireJob_SetsJobExpirationData()
        {
            UseSession(session =>
            {
                var insertTwoResult = InsertTwoJobs(session);

                Commit(session, x => x.ExpireJob(insertTwoResult.JobId1.ToString(), TimeSpan.FromDays(1)));

                var job = GetTestJob(session, insertTwoResult.JobId1.ToString());
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < job.ExpireAt &&
                            job.ExpireAt <= DateTime.UtcNow.AddDays(1));

                var anotherJob = GetTestJob(session, insertTwoResult.JobId2.ToString());
                Assert.Null(anotherJob.ExpireAt);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireList_SetsExpirationTime_OnAList_WithGivenKey()
        {
            const string arrangeSql = @"
insert into List (`Key`) values (@key)";

            UseSession(session =>
            {
                // Arrange
                session.Insert(new _List {Key = "list-1", Value = "1"});
                session.Insert(new _List {Key = "list-2", Value = "1"});
                session.Flush();

                // Act
                Commit(session, x => x.ExpireList("list-1", TimeSpan.FromMinutes(60)));

                // Assert
                var records = session.Query<_List>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["list-1"]);
                Assert.True(records["list-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["list-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireList_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireList(null, TimeSpan.FromSeconds(45))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireSet_SetsExpirationTime_OnASet_WithGivenKey()
        {
            UseSession(session =>
            {
                // Arrange
                session.Insert(new _Set {Key = "set-1", Value = "1"});
                session.Insert(new _Set {Key = "set-2", Value = "1"});
                session.Flush();

                // Act
                Commit(session, x => x.ExpireSet("set-1", TimeSpan.FromMinutes(60)));

                // Assert
                var records = session.Query<_Set>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["set-1"]);
                Assert.True(records["set-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["set-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void ExpireSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireSet(null, TimeSpan.FromSeconds(45))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void IncrementCounter_AddsRecordToCounterTable_WithPositiveValue()
        {
            UseSession(session =>
            {
                Commit(session, x => x.IncrementCounter("my-key"));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.Equal(null, record.ExpireAt);
            });
        }

        [Fact]
        [CleanDatabase]
        public void IncrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                });

                var recordCount = session.Query<_Counter>().Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void IncrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseSession(session =>
            {
                Commit(session, x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Fact]
        [CleanDatabase]
        public void InsertToList_AddsAnotherRecord_WhenBothKeyAndValueAreExist()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void InsertToList_AddsARecord_WithGivenValues()
        {
            UseSession(session =>
            {
                Commit(session, x => x.InsertToList("my-key", "my-value"));

                var record = session.Query<_List>().Single();


                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistHash_ClearsExpirationTime_OnAGivenHash()
        {
            const string arrangeSql = @"
insert into Hash (`Key`, `Field`, ExpireAt)
values (@key, @field, @expireAt)";

            UseSession(session =>
            {
                // Arrange
                session.Insert(
                    new _Hash {Key = "hash-1", Field = "field", ExpireAt = session.Storage.UtcNow.AddDays(1)});
                session.Insert(
                    new _Hash {Key = "hash-2", Field = "field", ExpireAt = session.Storage.UtcNow.AddDays(1)});

                // Act
                Commit(session, x => x.PersistHash("hash-1"));

                // Assert
                var records = session.Query<_Hash>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.Null(records["hash-1"]);
                Assert.NotNull(records["hash-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistHash(null)));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistJob_ClearsTheJobExpirationData()
        {
            UseSession(session =>
            {
                var insertTwoResult = InsertTwoJobs(session);

                Commit(session, x => x.PersistJob(insertTwoResult.JobId1.ToString()));

                var job = GetTestJob(session, insertTwoResult.JobId1.ToString());
                Assert.Null(job.ExpireAt);

                var anotherJob = GetTestJob(session, insertTwoResult.JobId2.ToString());
                Assert.NotNull(anotherJob.ExpireAt);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistList_ClearsExpirationTime_OnAGivenHash()
        {
            UseSession(session =>
            {
                // Arrange
                session.Insert(new _List {Key = "list-1", ExpireAt = session.Storage.UtcNow.AddDays(-1)});
                session.Insert(new _List {Key = "list-2", ExpireAt = session.Storage.UtcNow.AddDays(-1)});

                // Act
                Commit(session, x => x.PersistList("list-1"));

                // Assert
                var records = session.Query<_List>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.Null(records["list-1"]);
                Assert.NotNull(records["list-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistList_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistList(null)));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistSet_ClearsExpirationTime_OnAGivenHash()
        {
            UseSession(session =>
            {
                // Arrange
                session.Insert(new _Set {Key = "set-1", Value = "1", ExpireAt = session.Storage.UtcNow.AddDays(-1)});
                session.Insert(new _Set {Key = "set-2", Value = "1", ExpireAt = session.Storage.UtcNow.AddDays(-1)});

                // Act
                Commit(session, x => x.PersistSet("set-1"));

                // Assert
                var records = session.Query<_Set>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.Null(records["set-1"]);
                Assert.NotNull(records["set-2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void PersistSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistSet(null)));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameKey_ButDifferentValue()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameValue_ButDifferentKey()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromList_RemovesAllRecords_WithGivenKeyAndValue()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameKey_AndDifferentValue()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                });
                var recordCount = session.Query<_Set>().Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameValue_AndDifferentKey()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                });
                var recordCount = session.Query<_Set>().Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveFromSet_RemovesARecord_WithGivenKeyAndValue()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "my-value");
                });
                var recordCount = session.Query<_Set>().Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveHash_RemovesAllHashRecords()
        {
            UseSession(session =>
            {
                // Arrange
                Commit(session, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                }));

                // Act
                Commit(session, x => x.RemoveHash("some-hash"));

                // Assert
                var count = session.Query<_Hash>().Count();
                Assert.Equal(0, count);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.RemoveHash(null)));
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveSet_RemovesASet_WithAGivenKey()
        {
            UseSession(session =>
            {
                session.Insert(new _Set {Key = "set-1", Value = "1"});
                session.Insert(new _Set {Key = "set-2", Value = "1"});


                Commit(session, x => x.RemoveSet("set-1"));

                var record = session.Query<_Set>().Single();
                Assert.Equal("set-2", record.Key);
            });
        }

        [Fact]
        [CleanDatabase]
        public void RemoveSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.RemoveSet(null)));
            });
        }

        [Fact]
        [CleanDatabase]
        public void SetJobState_AppendsAStateAndSetItToTheJob()
        {
            UseSession(session =>
            {
                var insertTwoResult = InsertTwoJobs(session);

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> {{"Name", "Value"}});

                Commit(session, x => x.SetJobState(insertTwoResult.JobId1, state.Object));

                var job = GetTestJob(session, insertTwoResult.JobId1);
                Assert.Equal("State", job.StateName);
                Assert.NotNull(job.StateId);

                var anotherJob = GetTestJob(session, insertTwoResult.JobId2);
                Assert.Null(anotherJob.StateName);
                Assert.Null(anotherJob.StateId);

                var jobState = session.Query<_JobState>().Single();
                Assert.Equal(insertTwoResult.JobId1, jobState.Job.Id.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact]
        [CleanDatabase]
        public void SetRangeInHash_MergesAllRecords()
        {
            UseSession(session =>
            {
                Commit(session, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                }));

                var result = session.Query<_Hash>()
                    .Where(i => i.Key == "some-hash")
                    .ToDictionary(x => x.Field, x => x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Fact]
        [CleanDatabase]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.SetRangeInHash(null, new Dictionary<string, string>())));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull()
        {
            UseSession(session =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.SetRangeInHash("some-hash", null)));

                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }

        [Fact]
        [CleanDatabase]
        public void TrimList_RemovesAllRecords_IfStartFromGreaterThanEndingAt()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void TrimList_RemovesAllRecords_WhenStartingFromValue_GreaterThanMaxElementIndex()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void TrimList_RemovesRecords_OnlyOfAGivenKey()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void TrimList_RemovesRecordsToEnd_IfKeepAndingAt_GreaterThanMaxElementIndex()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.TrimList("my-key", 1, 100);
                });

                var recordCount = session.Query<_List>().Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        [CleanDatabase]
        public void TrimList_TrimsAList_ToASpecifiedRange()
        {
            UseSession(session =>
            {
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.InsertToList("my-key", "3");
                    x.TrimList("my-key", 1, 2);
                });

                var records = session.Query<_List>().ToArray();

                Assert.Equal(2, records.Length);
                Assert.Equal("1", records[0].Value);
                Assert.Equal("2", records[1].Value);
            });
        }
    }
}