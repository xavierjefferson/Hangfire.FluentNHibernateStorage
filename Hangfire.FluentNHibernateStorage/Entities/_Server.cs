using System;
using System.Data.SqlTypes;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Server
    {
        public virtual string Id { get; set; }
        public virtual string Data { get; set; }
        public virtual DateTime? LastHeartbeat { get; set; } 
    }
}