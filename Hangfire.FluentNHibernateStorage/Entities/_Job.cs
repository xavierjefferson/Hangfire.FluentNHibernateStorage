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
    public class _Job : IExpirableWithId
    {
        public _Job()
        {
            Parameters = new List<_JobParameter>();
            History = new List<_JobState>();
        }

        public virtual _JobState CurrentState { get; set; }
        public virtual string InvocationData { get; set; }
        public virtual string Arguments { get; set; }
        public virtual DateTime CreatedAt { get; set; }

        public virtual DateTime? FetchedAt { get; set; }

        public virtual IList<_JobParameter> Parameters { get; set; }
        public virtual IList<_JobState> History { get; set; }

        public virtual int Id { get; set; }
        public virtual DateTime? ExpireAt { get; set; }
    }
}