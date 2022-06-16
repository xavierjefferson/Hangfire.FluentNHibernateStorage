using System.Threading.Tasks;
using Hangfire.FluentNHibernate.SampleStuff;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public interface IHub
    {
        Task SendLogAsString(string message);

        Task SendLogAsObject(LogItem messageObject);
    }
}