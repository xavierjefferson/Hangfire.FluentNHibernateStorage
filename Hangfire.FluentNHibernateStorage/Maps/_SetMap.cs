using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _SetMap : Int32IdMapBase<_Set>
    {
        public _SetMap()
        {
            var indexName = $"UX_{Tablename}_{Constants.ColumnNames.Key}_{Constants.ColumnNames.Value}";
            Table("Set".WrapObjectName());
            //id is mapped in parent class
            this.MapStringKeyColumn().UniqueKey(indexName).Length(100);
            Map(i => i.Score).Column("Score".WrapObjectName()).Not.Nullable();
            this.MapStringValueColumn(false).UniqueKey(indexName).Length(255);
            this.MapExpireAt();
        }

        public override string Tablename => "Set";
    }

    /*
      CREATE TABLE [HangFire].[Set](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Score] [float] NOT NULL,
        [Value] [nvarchar](256) NOT NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Set]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Set_KeyAndValue] ON [HangFire].[Set] (
        [Key] ASC,
        [Value] ASC
    );
     
     */
}