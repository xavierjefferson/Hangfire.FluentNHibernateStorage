namespace Hangfire.FluentNHibernateStorage
{
    class StringToInt64Converter
    {
        public bool Valid { get; private set; }
        public long Value { get; private set; }

        public static StringToInt64Converter Convert(string jobId)
        {
            long tmp;
            var valid = long.TryParse(jobId, out tmp);
            return new StringToInt64Converter {Value = tmp, Valid = valid};
        }

        private StringToInt64Converter()
        {
            
        }
        
    }
}