using Hangfire.FluentNHibernateStorage.Entities;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _AggregatedCounterMap : _CounterBaseMap<_AggregatedCounter>
    {
        public override string Tablename => "AggregatedCounter";

        public override bool KeyIsUnique => true;
    }

    /*
     *
     CREATE TABLE [HangFire].[AggregatedCounter] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Value] [bigint] NOT NULL,
        [ExpireAt] [datetime] NULL,

        CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[AggregatedCounter]';

    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_CounterAggregated_Key] ON [HangFire].[AggregatedCounter] (
        [Key] ASC
    ) INCLUDE ([Value]);

     */
}