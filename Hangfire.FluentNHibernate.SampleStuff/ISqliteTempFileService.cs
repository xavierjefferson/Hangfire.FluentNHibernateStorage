namespace Hangfire.FluentNHibernate.SampleStuff
{
    public interface ISqliteTempFileService
    {
        string GetConnectionString();
        void CreateDatabase();
        string GetDatabaseFileName();
    }
}