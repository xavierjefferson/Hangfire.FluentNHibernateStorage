using System;
using System.Collections.Generic;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    /*
     * 
     * CREATE TABLE `Job` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `StateId` int(11) DEFAULT NULL,
 
  
  
  
  
  
  KEY `IX_Job_StateName` (`StateName`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
*/
    internal class _Job : IExpireWithId
    {
        public _Job()
        {
            Parameters = new List<_JobParameter>();
            SqlStates = new List<_JobState>();
        }

        public virtual _JobState State { get; set; }
        public virtual string InvocationData { get; set; }
        public virtual string Arguments { get; set; }
        public virtual DateTime CreatedAt { get; set; }

        public virtual DateTime? FetchedAt { get; set; }

        public virtual string StateName { get; set; }
        public virtual string StateReason { get; set; }
        public virtual string StateData { get; set; }
        public virtual IList<_JobParameter> Parameters { get; set; }
        public virtual IList<_JobState> SqlStates { get; set; }

        public virtual int Id { get; set; }
        public virtual DateTime? ExpireAt { get; set; }
    }
}