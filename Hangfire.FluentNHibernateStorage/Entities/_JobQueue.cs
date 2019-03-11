using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _JobQueue : Int32IdBase, IJobChild
    {
        public virtual string Queue { get; set; }
        public virtual DateTime? FetchedAt { get; set; }
        public virtual string FetchToken { get; set; }
        public virtual _Job Job { get; set; }
    }
}