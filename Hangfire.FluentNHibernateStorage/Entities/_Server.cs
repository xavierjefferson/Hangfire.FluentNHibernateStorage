using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal class _Server : EntityBase0
    {
        public virtual string Data { get; set; }
        public virtual DateTime LastHeartbeat { get; set; }
    }
}