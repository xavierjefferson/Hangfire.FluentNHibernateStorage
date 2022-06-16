using System;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public interface IJobMethods
    {
        void WriteSomething(string id, int currentCounter, int stage, int stages);
        void HelloWorld(string id, DateTime whenQueued, TimeSpan interval);
    }
}