namespace Hangfire.FluentNHibernateStorage
{
    class JobIdConverter
    {
        public bool Valid { get; private set; }
        public long Value { get; private set; }

        public static JobIdConverter Get(string jobId)
        {
            long tmp;
            var valid = long.TryParse(jobId, out tmp);
            return new JobIdConverter {Value = tmp, Valid = valid};
        }

        private JobIdConverter()
        {
            
        }
        
    }
}