using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _ListMap : Int32IdMapBase<_List>
    {
        public _ListMap()
        {
            //id is mapped in parent class
            this.MapStringKeyColumn().Length(100);
            this.MapStringValueColumn(true).Length(Constants.VarcharMaxLength);
            this.MapExpireAt();
        }

        public override string Tablename => "List";
    }

    /*
     
    CREATE TABLE [HangFire].[List](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Value] [nvarchar](max) NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

     */
}