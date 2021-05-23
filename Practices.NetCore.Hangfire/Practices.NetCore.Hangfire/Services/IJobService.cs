using System.Threading;
using System.Threading.Tasks;

namespace Practices.NetCore.Hangfire.Services
{
    public interface IJobService
    {
        Task<bool> SendAsync(string email);
        Task<bool> LongRunningProcess(string email, CancellationToken token);
        Task<bool> UnsubscribeUser(string email);
    }
}
