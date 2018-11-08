namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _JobParameter : Int32IdBase
    {
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual _Job Job { get; set; }
    }
}