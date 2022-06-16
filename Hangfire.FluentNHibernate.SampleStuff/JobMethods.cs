using System;
using System.Threading;
using Bogus;
using Microsoft.Extensions.Logging;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public class JobMethods : IJobMethods
    {
        private static readonly Random MyRandom = new Random();
        private static readonly Faker MyFaker = new Faker();

        private static volatile int _batch = 1;
        private static readonly object Mutex = new object();
        private readonly ILogger<JobMethods> _logger;

        public JobMethods(ILogger<JobMethods> logger)
        {
            _logger = logger;
        }

        public void WriteSomething(string id, int currentCounter, int stage, int stages)
        {
            _logger.LogInformation(
                $"Batch #{currentCounter}, scheduled by {id}, stage {stage}/{stages}, {MyFaker.Rant.Review()}");
            Thread.Sleep(TimeSpan.FromSeconds(MyRandom.Next(5, 60)));
        }

        public void HelloWorld(string id, DateTime whenQueued, TimeSpan interval)
        {
            _logger.LogInformation($"{id}: Enqueued={whenQueued}, Now={DateTime.Now}");
            EnqueueABatch(id);
        }

        private void EnqueueABatch(string id)
        {
            lock (Mutex)
            {
                string last = null;
                var stages = MyRandom.Next(1, 5);
                for (var stage = 0; stage < stages; stage++)
                    last = last != null
                        ? BackgroundJob.ContinueJobWith<IJobMethods>(last,
                            z => z.WriteSomething(id, _batch, stage + 1, stages))
                        : BackgroundJob.Enqueue<IJobMethods>(z => z.WriteSomething(id, _batch, stage + 1, stages));

                _batch++;
            }
        }

        public static void CreateRecurringJobs(ILogger logger)
        {
            var values = new[] {1, 2, 3, 5, 7, 11, 13, 17, 23, 29};
            foreach (var item in values)
            {
                var recurringJobId = $"HelloWorld-{item.ToString().PadLeft(3, '0')}";
                logger.LogInformation($"Adding job {recurringJobId}");
                var interval = TimeSpan.FromMinutes(item);

                RecurringJob.AddOrUpdate<IJobMethods>(recurringJobId,
                    x => x.HelloWorld(recurringJobId, DateTime.Now, interval),
                    $"*/{item} * * * *");
                logger.LogInformation($"Added job at {item} minute interval");
            }
        }
    }
}