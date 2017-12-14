using System;
using System.Threading;
using MySql.Data.MySqlClient;
using Xunit;

namespace Hangfire.FluentNHibernateStorage.Tests
{
    public class CountersAggregatorTests : IClassFixture<TestDatabaseFixture>, IDisposable
    {
        private readonly MySqlConnection _connection;
        private readonly FluentNHibernateStorage _storage;
        private readonly CountersAggregator _sut;

        public CountersAggregatorTests()
        {
            _connection = ConnectionUtils.CreateConnection();
            _storage = new FluentNHibernateStorage(_connection);
            _sut = new CountersAggregator(_storage, TimeSpan.Zero);
        }

        public void Dispose()
        {
            _connection.Dispose();
            _storage.Dispose();
        }

        [Fact]
        [CleanDatabase]
        public void CountersAggregatorExecutesProperly()
        {
            const string createSql = @"
insert into Counter (`Key`, Value, ExpireAt) 
values ('key', 1, @expireAt)";

            _storage.UseConnection(connection =>
            {
                // Arrange
                connection.Execute(createSql, new {expireAt = DateTime.UtcNow.AddHours(1)});

                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                _sut.Execute(cts.Token);

                // Assert
                Assert.Equal(1, connection.Query<int>(@"select count(*) from AggregatedCounter").Single());
            });
        }
    }
}