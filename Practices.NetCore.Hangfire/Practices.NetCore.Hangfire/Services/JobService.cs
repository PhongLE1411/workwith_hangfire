using System;
using System.Threading;
using System.Threading.Tasks;

namespace Practices.NetCore.Hangfire.Services
{
    public class JobService : IJobService
    {
        public async Task<bool> LongRunningProcess(string email, CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), token);

            return true;
        }

        public Task<bool> SendAsync(string email)
        {

            return Task.FromResult(true);
        }

        public Task<bool> UnsubscribeUser(string email)
        {
            Console.WriteLine($"Unsubscribed {email}");
            return Task.FromResult(true);
        }
    }
}
