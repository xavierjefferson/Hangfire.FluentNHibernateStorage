namespace Hangfire.FluentNHibernateStorage
{
    internal class StringToInt32Converter
    {
        private StringToInt32Converter()
        {
        }

        public bool Valid { get; private set; }
        public int Value { get; private set; }

        public static StringToInt32Converter Convert(string jobId)
        {
            int value;
            var valid = int.TryParse(jobId, out value);
            return new StringToInt32Converter {Value = value, Valid = valid};
        }
    }
}