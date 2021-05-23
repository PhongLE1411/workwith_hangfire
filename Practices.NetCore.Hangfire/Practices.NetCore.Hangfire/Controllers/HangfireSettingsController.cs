using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Practices.NetCore.Hangfire.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Practices.NetCore.Hangfire.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HangfireSettingsController : ControllerBase
    {
        private readonly IJobService _emailService;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        public HangfireSettingsController(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, IJobService emailService)
        {
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
            _emailService = emailService;
        }

        [HttpPost]
        [Route("FireAndForget")]
        public IActionResult FireAndForget(string email)
        {
            var jobId = _backgroundJobClient.Enqueue<IJobService>((x) => x.SendAsync(email));

            return Ok($"Job Id {jobId} Completed. Welcome Mail Sent!");
        }

        [HttpPost]
        [Route("LongRunningProcess")]
        public IActionResult LongRunningProcess(string email, CancellationToken token)
        {
            //https://docs.hangfire.io/en/latest/background-methods/using-cancellation-tokens.html
            var jobId = _backgroundJobClient.Enqueue<IJobService>((x) => x.LongRunningProcess(email, token));

            return Ok($"Job Id {jobId} Completed. Long Running Process of Mail is sent!");
        }

        [HttpPost]
        [Route("Delay")]
        public IActionResult Delay(string email, int delayTimeInSecond)
        {
            var jobId = _backgroundJobClient.Schedule<IJobService>((x) => x.SendAsync(email), TimeSpan.FromSeconds(delayTimeInSecond));
            return Ok($"Job Id {jobId} Completed. Delayed Welcome Mail Sent!. Mail is sent after {delayTimeInSecond} seconds");
        }

        [HttpPost]
        [Route("Recurring")]
        public IActionResult Recurring(string email, string cron, string jobName)//Cron.Monthly
        {
            _recurringJobManager.AddOrUpdate<IJobService>(jobName, (x) => x.SendAsync(email), cron);

            return Ok($"Recurring Job Scheduled. Email will be mailed Monthly for {email}!");
        }

        [HttpPost]
        [Route("Continuation")]
        public IActionResult Continuation(string email)
        {
            var jobId = _backgroundJobClient.Enqueue<IJobService>((x) => x.UnsubscribeUser(email));
            _backgroundJobClient.ContinueJobWith<IJobService>(jobId, (x) => x.SendAsync(email));

            return Ok($"Job Id {jobId} Completed. User is unsubcribed and email is sent!");
        }
    }
}
