using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.AutocompleteApi.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private int _executionCount = 0;
        private Timer _timer;
        private readonly IMemoryStoreService _memoryStoreService;

        public TimedHostedService(IMemoryStoreService memoryStoreService)
        {
            _memoryStoreService = memoryStoreService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // doesnt need to run first day, since values are freshly loaded
            // schedule to start at 20:00 (19:00/21:00) depending on saving time
            var firstRunTime = DateTime.Today.AddDays(1).AddHours(20);


            // run once per 24 hours
            TimeSpan interval = TimeSpan.FromHours(24);

            // safety if someone changes nextRunTime too close to run time
            // expecting that calculation can run for 4 hours
            var dueTime = firstRunTime - DateTime.Now;
            if (dueTime.Hours < 4)
            {
                dueTime = dueTime.Add(TimeSpan.FromHours(24));
            }

            _timer = new Timer(DoWork, null, dueTime, interval);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            //fire and forget
#pragma warning disable 4014
            _memoryStoreService.GenerateAll();
#pragma warning restore 4014
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}