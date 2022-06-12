using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _HashMap : Int32IdMapBase<_Hash>
    {
        public _HashMap()
        {
            var indexName = $"UX_{Tablename}_{Constants.ColumnNames.Key}_Field";

            //id is mapped in parent class
            this.MapStringKeyColumn().UniqueKey(indexName).Length(100);
            Map(i => i.Field).Column("Field".WrapObjectName()).Not.Nullable().UniqueKey(indexName).Length(40);
            this.MapStringValueColumn(true).Length(Constants.VarcharMaxLength);
            this.MapExpireAt();
        }

        public override string Tablename => "Hash";
    }

    /*
    We're only doing one value column instead of two.


    CREATE TABLE [HangFire].[Hash](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [StringValue] [nvarchar](max) NULL,
        [IntValue] [int] NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Hash]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_KeyAndName] ON [HangFire].[Hash] (
        [Key] ASC,
        [Name] ASC
    );
    */
}