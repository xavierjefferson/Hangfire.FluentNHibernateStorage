using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Tests.Base.Fixtures;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Base.Misc
{
    public abstract class WriteOnlyTransactionTestsBase : TestBase
    {
        protected WriteOnlyTransactionTestsBase(DatabaseFixtureBase fixture) : base(fixture)
        {
            var defaultProvider = new Mock<IPersistentJobQueueProvider>();
            defaultProvider.Setup(x => x.GetJobQueue())
                .Returns(new Mock<IPersistentJobQueue>().Object);
        }


        private class InsertTwoJobsResult
        {
            public string JobId1 { get; set; }
            public string JobId2 { get; set; }
        }

        private static InsertTwoJobsResult InsertTwoJobs(StatelessSessionWrapper session, Action<_Job> action = null)
        {
            var insertTwoJobsResult = new InsertTwoJobsResult();


            for (var i = 0; i < 2; i++)
            {
                var newJob = InsertNewJob(session, action);

                if (i == 0)
                    insertTwoJobsResult.JobId1 = newJob.Id.ToString();
                else
                    insertTwoJobsResult.JobId2 = newJob.Id.ToString();
            }

            return insertTwoJobsResult;
        }

        private static _Job GetTestJob(StatelessSessionWrapper connection, string jobId)
        {
            return connection.Query<_Job>().Single(i => i.Id == long.Parse(jobId));
        }

        private void Commit(
            StatelessSessionWrapper connection,
            Action<FluentNHibernateWriteOnlyTransaction> action)
        {
            using (var transaction = new FluentNHibernateWriteOnlyTransaction(connection.Storage))
            {
                action(transaction);
                transaction.Commit();
            }
        }

        [Fact]
        public void AddJobState_JustAddsANewRecordInATable()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                var newJob = InsertNewJob(session);

                var jobId = newJob.Id;

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> {{"Name", "Value"}});

                Commit(session, x => x.AddJobState(jobId.ToString(), state.Object));

                var job = GetTestJob(session, jobId.ToString());
                Assert.Null(job.StateName);


                var jobState = session.Query<_JobState>().Single();

                Assert.Equal(jobId, jobState.Job.Id);
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.InRange(connection.Storage.UtcNow.Subtract(jobState.CreatedAt).TotalSeconds, 0, 10);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact]
        public void AddRangeToSet_AddsAllItems_ToAGivenSet()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var items = new List<string> {"1", "2", "3"};

                Commit(session, x => x.AddRangeToSet("my-set", items));

                var records = session.Query<_Set>().Where(i => i.Key == "my-set").Select(i => i.Value).ToList();
                Assert.Equal(items, records);
            });
        }

        [Fact]
        public void AddRangeToSet_ThrowsAnException_WhenItemsValueIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.AddRangeToSet("my-set", null)));

                Assert.Equal("items", exception.ParamName);
            });
        }

        [Fact]
        public void AddRangeToSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.AddRangeToSet(null, new List<string>())));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void AddToQueue_CallsEnqueue_OnTargetPersistentQueue()
        {
            var correctJobQueue = new Mock<IPersistentJobQueue>();
            var correctProvider = new Mock<IPersistentJobQueueProvider>();
            correctProvider.Setup(x => x.GetJobQueue())
                .Returns(correctJobQueue.Object);


            UseJobStorageConnectionWithSession((session, connection) =>
            {
                connection.Storage.QueueProviders.Add(correctProvider.Object, new[] {"default"});
                var job = InsertNewJob(session);
                Commit(session, x => x.AddToQueue("default", job.Id.ToString()));

                correctJobQueue.Verify(x =>
                    x.Enqueue(It.IsNotNull<StatelessSessionWrapper>(), "default", job.Id.ToString()));
            });
        }

        [Fact]
        public void AddToSet_AddsARecord_IfThereIsNo_SuchKeyAndValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                Commit(session, x => x.AddToSet("my-key", "my-value"));

                var record = session.Query<_Set>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(0.0, record.Score, 2);
            });
        }

        [Fact]
        public void AddToSet_AddsARecord_WhenKeyIsExists_ButValuesAreDifferent()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void AddToSet_DoesNotAddARecord_WhenBothKeyAndValueAreExist()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void AddToSet_WithScore_AddsARecordWithScore_WhenBothKeyAndValueAreNotExist()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                Commit(session, x => x.AddToSet("my-key", "my-value", 3.2));

                var record = session.Query<_Set>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(3.2, record.Score, 3);
            });
        }

        [Fact]
        public void AddToSet_WithScore_UpdatesAScore_WhenBothKeyAndValueAreExist()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void DecrementCounter_AddsRecordToCounterTable_WithNegativeValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                Commit(session, x => x.DecrementCounter("my-key"));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.Null(record.ExpireAt);
            });
        }

        [Fact]
        public void DecrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void DecrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                Commit(session, x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)));

                var record = session.Query<_Counter>().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(session.Storage.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < session.Storage.UtcNow.AddHours(25));
            });
        }

        [Fact]
        public void ExpireHash_SetsExpirationTimeOnAHash_WithGivenKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _Hash {Key = "hash-1", Field = "field"});
                session.Insert(new _Hash {Key = "hash-2", Field = "field"});
                //does nothing

                // Act
                Commit(session, x => x.ExpireHash("hash-1", TimeSpan.FromMinutes(60)));

                // Assert

                var records = session.Query<_Hash>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(session.Storage.UtcNow.AddMinutes(59) < records["hash-1"]);
                Assert.True(records["hash-1"] < session.Storage.UtcNow.AddMinutes(61));
                Assert.Null(records["hash-2"]);
            });
        }

        [Fact]
        public void ExpireHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireHash(null, TimeSpan.FromMinutes(5))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void ExpireJob_SetsJobExpirationData()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                var insertTwoResult = InsertTwoJobs(session);

                Commit(session, x => x.ExpireJob(insertTwoResult.JobId1, TimeSpan.FromDays(1)));
                //Act

                var job = GetTestJob(session, insertTwoResult.JobId1);
                //Assert
                Assert.True(session.Storage.UtcNow.AddMinutes(-1) < job.ExpireAt &&
                            job.ExpireAt <= session.Storage.UtcNow.AddDays(1));

                var anotherJob = GetTestJob(session, insertTwoResult.JobId2);
                Assert.Null(anotherJob.ExpireAt);
            });
        }

        [Fact]
        public void ExpireList_SetsExpirationTime_OnAList_WithGivenKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _List {Key = "list-1", Value = "1"});
                session.Insert(new _List {Key = "list-2", Value = "1"});
                //does nothing

                // Act
                Commit(session, x => x.ExpireList("list-1", TimeSpan.FromMinutes(60)));

                // Assert

                var records = session.Query<_List>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(session.Storage.UtcNow.AddMinutes(59) < records["list-1"]);
                Assert.True(records["list-1"] < session.Storage.UtcNow.AddMinutes(61));
                Assert.Null(records["list-2"]);
            });
        }

        [Fact]
        public void ExpireList_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireList(null, TimeSpan.FromSeconds(45))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void ExpireSet_SetsExpirationTime_OnASet_WithGivenKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _Set {Key = "set-1", Value = "1"});
                session.Insert(new _Set {Key = "set-2", Value = "1"});
                //does nothing

                // Act
                Commit(session, x => x.ExpireSet("set-1", TimeSpan.FromMinutes(60)));

                // Assert

                var records = session.Query<_Set>()
                    .ToDictionary(x => x.Key, x => x.ExpireAt);
                Assert.True(session.Storage.UtcNow.AddMinutes(59) < records["set-1"]);
                Assert.True(records["set-1"] < session.Storage.UtcNow.AddMinutes(61));
                Assert.Null(records["set-2"]);
            });
        }

        [Fact]
        public void ExpireSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.ExpireSet(null, TimeSpan.FromSeconds(45))));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void IncrementCounter_AddsRecordToCounterTable_WithPositiveValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x => x.IncrementCounter("my-key"));
                //Act
                var record = session.Query<_Counter>().Single();
                //Assert
                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.Null(record.ExpireAt);
            });
        }

        [Fact]
        public void IncrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                });
                //Act
                var recordCount = session.Query<_Counter>().Count();
                //Assert

                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        public void IncrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)));
                //Act
                var record = session.Query<_Counter>().Single();
                //Assert
                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(session.Storage.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < session.Storage.UtcNow.AddHours(25));
            });
        }

        [Fact]
        public void InsertToList_AddsAnotherRecord_WhenBothKeyAndValueAreExist()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                });
                //Act
                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        public void InsertToList_AddsARecord_WithGivenValues()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x => x.InsertToList("my-key", "my-value"));
                //Act
                var record = session.Query<_List>().Single();

                //Assert
                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
            });
        }

        [Fact]
        public void PersistHash_ClearsExpirationTime_OnAGivenHash()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void PersistHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistHash(null)));
                //Assert

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void PersistJob_ClearsTheJobExpirationData()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                var insertTwoResult = InsertTwoJobs(session,
                    item => { item.ExpireAt = item.CreatedAt = session.Storage.UtcNow; });

                Commit(session, x => x.PersistJob(insertTwoResult.JobId1));

                //Act

                var job = GetTestJob(session, insertTwoResult.JobId1);
                //Assert
                Assert.Null(job.ExpireAt);

                var anotherJob = GetTestJob(session, insertTwoResult.JobId2);
                Assert.NotNull(anotherJob.ExpireAt);
            });
        }

        [Fact]
        public void PersistList_ClearsExpirationTime_OnAGivenHash()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _List {Key = "list-1", ExpireAt = session.Storage.UtcNow.AddDays(-1)});
                session.Insert(new _List {Key = "list-2", ExpireAt = session.Storage.UtcNow.AddDays(-1)});
                //does nothing
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
        public void PersistList_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistList(null)));
                //Assert

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void PersistSet_ClearsExpirationTime_OnAGivenHash()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void PersistSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.PersistSet(null)));
                //Assert

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameKey_ButDifferentValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                });

                //Act

                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameValue_ButDifferentKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                });
                //Act

                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        public void RemoveFromList_RemovesAllRecords_WithGivenKeyAndValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                });
                //Act

                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameKey_AndDifferentValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                });
                //Act

                var recordCount = session.Query<_Set>().Count();
                //Assert
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameValue_AndDifferentKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                });
                //Act

                var recordCount = session.Query<_Set>().Count();
                //Assert
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        public void RemoveFromSet_RemovesARecord_WithGivenKeyAndValue()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Arrange
                Commit(session, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "my-value");
                });
                //Act

                var recordCount = session.Query<_Set>().Count();
                //Assert
                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        public void RemoveHash_RemovesAllHashRecords()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
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
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Assert
                Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.RemoveHash(null)));
            });
        }

        [Fact]
        public void RemoveSet_RemovesASet_WithAGivenKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                session.Insert(new _Set {Key = "set-1", Value = "1"});
                session.Insert(new _Set {Key = "set-2", Value = "1"});


                Commit(session, x => x.RemoveSet("set-1"));
                // Act

                var record = session.Query<_Set>().Single();
                //Assert
                Assert.Equal("set-2", record.Key);
            });
        }

        [Fact]
        public void RemoveSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                //Assert

                Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.RemoveSet(null)));
            });
        }

        [Fact]
        public void SetJobState_AppendsAStateAndSetItToTheJob()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                var insertTwoResult = InsertTwoJobs(session);

                var state = new Mock<IState>();
                const string expected = "State";
                state.Setup(x => x.Name).Returns(expected);
                const string reason = "Reason";
                state.Setup(x => x.Reason).Returns(reason);
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> {{"Name", "Value"}});

                Commit(session, x => x.SetJobState(insertTwoResult.JobId1, state.Object));
                // Act

                var job = GetTestJob(session, insertTwoResult.JobId1);
                //Assert
                Assert.Equal(expected, job.StateName);


                var anotherJob = GetTestJob(session, insertTwoResult.JobId2);
                Assert.Null(anotherJob.StateName);


                var jobState = session.Query<_JobState>().Single();
                Assert.Equal(insertTwoResult.JobId1, jobState.Job.Id.ToString());
                Assert.Equal(expected, jobState.Name);
                Assert.Equal(reason, jobState.Reason);
                Assert.InRange(connection.Storage.UtcNow.Subtract(jobState.CreatedAt).TotalSeconds, 0, 10);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Fact]
        public void SetRangeInHash_MergesAllRecords()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                Commit(session, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                }));
                // Act

                //Assert
                var result = session.Query<_Hash>()
                    .Where(i => i.Key == "some-hash")
                    .ToDictionary(x => x.Field, x => x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Fact]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.SetRangeInHash(null, new Dictionary<string, string>())));
                //Assert

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(session, x => x.SetRangeInHash("some-hash", null)));
                //Assert
                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }

        [Fact]
        public void TrimList_RemovesAllRecords_IfStartFromGreaterThanEndingAt()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                });
                // Act

                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        public void TrimList_RemovesAllRecords_WhenStartingFromValue_GreaterThanMaxElementIndex()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange
                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                });
                // Act

                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(0, recordCount);
            });
        }

        [Fact]
        public void TrimList_RemovesRecords_OnlyOfAGivenKey()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange

                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                });
                // Act


                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(1, recordCount);
            });
        }

        [Fact]
        public void TrimList_RemovesRecordsToEnd_IfKeepAndingAt_GreaterThanMaxElementIndex()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange

                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.TrimList("my-key", 1, 100);
                });
                // Act


                var recordCount = session.Query<_List>().Count();
                //Assert
                Assert.Equal(2, recordCount);
            });
        }

        [Fact]
        public void TrimList_TrimsAList_ToASpecifiedRange()
        {
            UseJobStorageConnectionWithSession((session, connection) =>
            {
                // Arrange

                Commit(session, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.InsertToList("my-key", "3");
                    x.TrimList("my-key", 1, 2);
                });
                // Act


                var records = session.Query<_List>().ToArray();
                //Assert
                Assert.Equal(2, records.Length);
                Assert.Equal("1", records[0].Value);
                Assert.Equal("2", records[1].Value);
            });
        }
    }
}