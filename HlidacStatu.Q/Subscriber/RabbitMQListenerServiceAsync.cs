﻿using EasyNetQ;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Q.Subscriber
{
    public class RabbitMQListenerServiceAsync<T> : IHostedService where T : class
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger _logger;
        private readonly IMessageHandlerAsync<T> _messageHandler;
        private readonly RabbitMQOptions _options;
        private IBus _rabbitBus;

        public RabbitMQListenerServiceAsync(
            IHostApplicationLifetime appLifetime,
            ILogger<RabbitMQListenerServiceAsync<T>> logger,
            IMessageHandlerAsync<T> messageHandler,
            IOptionsMonitor<RabbitMQOptions> options)
        {
            _appLifetime = appLifetime;
            _logger = logger;
            _messageHandler = messageHandler;
            _options = options.CurrentValue;
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                _logger.LogError("RabbitMQ connection string is missing.");
                throw new Exception("Missing connection string.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to RabbitMQ.");
            try
            {
                _rabbitBus = RabbitHutch.CreateBus(_options.ConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection failed.");
                throw;
            }
            _logger.LogInformation("Connected.");
            _appLifetime.ApplicationStarted.Register(SubscribeToQueue);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Disconnecting from RabbitMQ.");
            _rabbitBus.Dispose();
            _logger.LogInformation("Disconnected.");
            return Task.CompletedTask;
        }

        private void SubscribeToQueue()
        {
            _logger.LogInformation("Subscribing to Queue.");
            _logger.LogInformation(_options.ToString());
            _rabbitBus.PubSub.SubscribeAsync<T>(_options.SubscriberName, _messageHandler.HandleAsync, configure =>
            {
                configure.WithPrefetchCount(_options.PrefetchCount);
            });
        }
    }
}
