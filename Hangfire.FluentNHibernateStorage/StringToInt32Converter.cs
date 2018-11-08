namespace Hangfire.FluentNHibernateStorage
{
    class StringToInt32Converter
    {
        public bool Valid { get; private set; }
        public int Value { get; private set; }

        public static StringToInt32Converter Convert(string jobId)
        {
            int tmp;
            var valid = int.TryParse(jobId, out tmp);
            return new StringToInt32Converter {Value = tmp, Valid = valid};
        }

        private StringToInt32Converter()
        {
            
        }
        
    }
}