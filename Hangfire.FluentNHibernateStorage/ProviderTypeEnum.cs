namespace Hangfire.FluentNHibernateStorage
{
    public enum ProviderTypeEnum
    {
        None = 0,
      
        OracleClient10 = 3,
        OracleClient9 = 4,
        PostgreSQLStandard = 5,
        PostgreSQL81 = 6,
        PostgreSQL82 = 7,
        Firebird = 8,
       
        DB2Informix1150 = 10,
        DB2Standard = 11,
        MySQL = 12,
        MsSql2008 = 13,
        MsSql2012 = 14,
        MsSql2005 = 15,
        MsSql2000 = 16
    }
}