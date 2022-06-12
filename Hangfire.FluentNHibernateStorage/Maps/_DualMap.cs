using FluentNHibernate.Mapping;
using Hangfire.FluentNHibernateStorage.Entities;
using Hangfire.FluentNHibernateStorage.Extensions;

namespace Hangfire.FluentNHibernateStorage.Maps
{
    internal class _DualMap : ClassMapBase<_Dual>
    {
        public _DualMap()
        {
            Id(i => i.Id).Column(Constants.ColumnNames.Id.WrapObjectName()).GeneratedBy.Assigned();
        }

        public override string Tablename
        {
            get { return "Dual"; }
        }
    }
}