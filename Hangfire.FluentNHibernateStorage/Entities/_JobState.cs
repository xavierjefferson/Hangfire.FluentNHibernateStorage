using System;
using Hangfire.FluentNHibernateStorage.Maps;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _JobState : Int32IdBase, IJobChild, ICreatedAt
    {
        public virtual string Name { get; set; }
        public virtual string Reason { get; set; }
        public virtual DateTime CreatedAt { get; set; }
        public virtual string Data { get; set; }
        public virtual _Job Job { get; set; }
    }
}