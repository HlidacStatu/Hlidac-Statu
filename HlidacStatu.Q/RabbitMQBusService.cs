using EasyNetQ;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Q
{
    public class RabbitMQBusService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly RabbitMQBusOptions _options;
        public IBus RabbitBus { get; private set; }

        public RabbitMQBusService(
            IHostApplicationLifetime appLifetime,
            IOptionsMonitor<RabbitMQBusOptions> options)
        {
            _appLifetime = appLifetime;
            _options = options.CurrentValue;
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new Exception("Missing connection string.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RabbitBus = RabbitHutch.CreateBus(_options.ConnectionString);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            RabbitBus.Dispose();
            return Task.CompletedTask;
        }

    }
}