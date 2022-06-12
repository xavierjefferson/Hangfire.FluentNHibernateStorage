using System;
using System.Collections.Generic;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Job : IExpirableWithId, ICreatedAt, IExpirable
    {
        public _Job()
        {
            Parameters = new List<_JobParameter>();
            History = new List<_JobState>();
        }

        // public virtual _JobState CurrentState { get; set; }
        public virtual string InvocationData { get; set; }

        public virtual string Arguments { get; set; }

        public virtual IList<_JobParameter> Parameters { get; set; }
        public virtual IList<_JobState> History { get; set; }
        public virtual string StateName { get; set; }
        public virtual string StateReason { get; set; }
        public virtual DateTime? LastStateChangedAt { get; set; }
        public virtual string StateData { get; set; }
        public virtual DateTime CreatedAt { get; set; }


        public virtual int Id { get; set; }
        public virtual DateTime? ExpireAt { get; set; }
    }
}