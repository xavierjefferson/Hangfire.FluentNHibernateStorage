using System;
using System.Collections.Generic;
using System.Transactions;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.JobQueue;
using Hangfire.FluentNHibernateStorage.Monitoring;
using Hangfire.Storage.Monitoring;
using Moq;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests.Monitoring
{
    public class FluentNHibernateMonitoringApiTests : IClassFixture<TestDatabaseFixture>, IDisposable
    {
        public FluentNHibernateMonitoringApiTests()
        {
            var persistenceConfigurer = ConnectionUtils.GetPersistenceConfigurer();


            var persistentJobQueueMonitoringApiMock = new Mock<IPersistentJobQueueMonitoringApi>();
            persistentJobQueueMonitoringApiMock.Setup(m => m.GetQueues()).Returns(new[] {"default"});

            var defaultProviderMock = new Mock<IPersistentJobQueueProvider>();
            defaultProviderMock.Setup(m => m.GetJobQueueMonitoringApi())
                .Returns(persistentJobQueueMonitoringApiMock.Object);

            var storageMock = new Mock<FluentNHibernateJobStorage>(persistenceConfigurer);
            storageMock
                .Setup(m => m.QueueProviders)
                .Returns(new PersistentJobQueueProviderCollection(defaultProviderMock.Object));

            _storage = storageMock.Object;
            _sut = new FluentNHibernateMonitoringApi(_storage);
            _createdAt = _storage.UtcNow;
            _expireAt = _storage.UtcNow.AddMinutes(1);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }

        private readonly string _arguments = "[\"test\"]";

        private readonly DateTime _createdAt;
        private readonly DateTime _expireAt;

        private readonly string _invocationData =
            "{\"Type\":\"System.Console, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"," +
            "\"Method\":\"WriteLine\"," +
            "\"ParameterTypes\":\"[\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\\\"]\"," +
            "\"Arguments\":\"[\\\"\\\"test\\\"\\\"]\"}";

        private readonly FluentNHibernateJobStorage _storage;
        private readonly FluentNHibernateMonitoringApi _sut;

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnDeletedCount()
        {
            const int expectedStatsDeletedCount = 7;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                connection.Insert(new _AggregatedCounter {Key = "stats:deleted", Value = 5});
                connection.Insert(new _Counter {Key = "stats:deleted", Value = 1});
                connection.Insert(new _Counter {Key = "stats:deleted", Value = 1});
                connection.Flush();
                connection.Clear();
                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedStatsDeletedCount, result.Deleted);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnEnqueuedCount()
        {
            const int expectedEnqueuedCount = 1;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                connection.Insert(new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    StateName = "Enqueued",
                    CreatedAt = connection.Storage.UtcNow
                });
                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedEnqueuedCount, result.Enqueued);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnFailedCount()
        {
            const int expectedFailedCount = 2;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                for (var i = 0; i < 2; i++)
                {
                    connection.Insert(new _Job
                    {
                        InvocationData = string.Empty,
                        Arguments = string.Empty,
                        CreatedAt = connection.Storage.UtcNow,
                        StateName = "Failed"
                    });
                }

                connection.Flush();

                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedFailedCount, result.Failed);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnProcessingCount()
        {
            const int expectedProcessingCount = 1;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                connection.Insert(new _Job
                {
                    InvocationData = string.Empty,
                    Arguments = string.Empty,
                    CreatedAt = _storage.UtcNow,
                    StateName = "Processing"
                });
                connection.Flush();

                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedProcessingCount, result.Processing);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnQueuesCount()
        {
            const int expectedQueuesCount = 1;

            var result = _sut.GetStatistics();

            Assert.Equal(expectedQueuesCount, result.Queues);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnRecurringCount()
        {
            const int expectedRecurringCount = 1;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                connection.Insert(new _Set {Key = "recurring-jobs", Value = "test", Score = 0});
                connection.Flush();
                connection.Clear();
                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedRecurringCount, result.Recurring);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnScheduledCount()
        {
            const int expectedScheduledCount = 3;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                for (var i = 0; i < 3; i++)
                {
                    connection.Insert(new _Job
                    {
                        InvocationData = string.Empty,
                        CreatedAt = connection.Storage.UtcNow,
                        Arguments = string.Empty,
                        StateName = "Scheduled"
                    });
                }

                connection.Flush();

                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedScheduledCount, result.Scheduled);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnServersCount()
        {
            const int expectedServersCount = 2;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                for (var i = 1; i < 3; i++)
                {
                    connection.Insert(new _Server {Id = i.ToString(), Data = i.ToString()});
                }

                connection.Flush();
                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedServersCount, result.Servers);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void GetStatistics_ShouldReturnSucceededCount()
        {
            const int expectedStatsSucceededCount = 11;

            StatisticsDto result = null;
            _storage.UseSession(connection =>
            {
                connection.Insert(new _Counter {Key = "stats:succeeded", Value = 1});
                connection.Insert(new _AggregatedCounter {Key = "stats:succeeded", Value = 10});
                connection.Flush();
                result = _sut.GetStatistics();
            });

            Assert.Equal(expectedStatsSucceededCount, result.Succeeded);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void JobDetails_ShouldReturnCreatedAtAndExpireAt()
        {
            JobDetailsDto result = null;

            _storage.UseSession(connection =>
            {
                var newJob = new _Job
                {
                    CreatedAt = _createdAt,
                    InvocationData = _invocationData,
                    Arguments = _arguments,
                    ExpireAt = _expireAt
                };
                connection.Insert(newJob);
                connection.Flush();
                var jobId = newJob.Id;

                result = _sut.JobDetails(jobId.ToString());
            });

            Assert.Equal(_createdAt.ToString("yyyy-MM-dd hh:mm:ss"),
                result.CreatedAt.Value.ToString("yyyy-MM-dd hh:mm:ss"));
            Assert.Equal(_expireAt.ToString("yyyy-MM-dd hh:mm:ss"),
                result.ExpireAt.Value.ToString("yyyy-MM-dd hh:mm:ss"));
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void JobDetails_ShouldReturnHistory()
        {
            const string jobStateName = "Scheduled";
            const string stateData =
                "{\"EnqueueAt\":\"2016-02-21T11:56:05.0561988Z\", \"ScheduledAt\":\"2016-02-21T11:55:50.0561988Z\"}";

            JobDetailsDto result = null;

            _storage.UseSession(connection =>
            {
                var newJob = new _Job
                {
                    CreatedAt = _createdAt,
                    InvocationData = _invocationData,
                    Arguments = _arguments,
                    ExpireAt = _expireAt
                };
                connection.Insert(newJob);
                connection.Insert(new _JobState
                {
                    Job = newJob,
                    CreatedAt = _createdAt,
                    Name = jobStateName,
                    Data = stateData
                });
                connection.Flush();
                var jobId = newJob.Id;

                result = _sut.JobDetails(jobId.ToString());
            });

            Assert.Equal(1, result.History.Count);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void JobDetails_ShouldReturnJob()
        {
            JobDetailsDto result = null;

            _storage.UseSession(connection =>
            {
                var newJob = new _Job
                {
                    CreatedAt = _createdAt,
                    InvocationData = _invocationData,
                    Arguments = _arguments,
                    ExpireAt = _expireAt
                };
                connection.Insert(newJob);
                connection.Flush();
                var jobId = newJob.Id;


                result = _sut.JobDetails(jobId.ToString());
            });

            Assert.NotNull(result.Job);
        }

        [Fact]
        [CleanDatabase(IsolationLevel.ReadUncommitted)]
        public void JobDetails_ShouldReturnProperties()
        {
            var properties = new Dictionary<string, string>
            {
                ["CurrentUICulture"] = "en-US",
                ["CurrentCulture"] = "lt-LT"
            };

            JobDetailsDto result = null;

            _storage.UseSession(connection =>
            {
                var newJob = new _Job
                {
                    CreatedAt = _createdAt,
                    InvocationData = _invocationData,
                    Arguments = _arguments,
                    ExpireAt = _expireAt
                };
                connection.Insert(newJob);

                foreach (var x in properties)
                {
                    connection.Insert(new _JobParameter {Job = newJob, Name = x.Key, Value = x.Value});
                }

                connection.Flush();
                var jobId = newJob.Id;

                result = _sut.JobDetails(jobId.ToString());
            });

            Assert.Equal(properties, result.Properties);
        }
    }
}