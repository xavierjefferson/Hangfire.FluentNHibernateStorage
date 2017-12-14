using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal interface IExpireWithKey:IExpirable
    {
        string Key { get; set; }
    }
}