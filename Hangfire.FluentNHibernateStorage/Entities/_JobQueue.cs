using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal class _JobQueue : EntityBase0
    {
        public virtual _Job Job { get; set; }
        public virtual string Queue { get; set; }
        public virtual DateTime? FetchedAt { get; set; }
        public virtual string FetchToken { get; set; }
    }
}