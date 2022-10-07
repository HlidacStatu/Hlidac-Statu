using HlidacStatu.Q.Messages;
using HlidacStatu.Q.Subscriber;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http.Headers;
using HlidacStatu.LibCore.Extensions;

namespace HlidacStatu.ClassificationRepair
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureHostForDocker(tag:"classificationrepair")
            .ConfigureServices((hostContext, services) =>
            {
                // rabbit configuration
                services.Configure<RabbitMQOptions>(options =>
                {
                    options.ConnectionString =
                        hostContext.Configuration.GetValue<string>("AppSettings:RabbitMqConnectionString");
                    options.PrefetchCount = hostContext.Configuration.GetValue<ushort>("PrefetchCount");
                    options.SubscriberName = hostContext.Configuration.GetValue<string>("SubscriberName");
                });
                services.AddHostedService<RabbitMQListenerServiceAsync<ClassificationFeedback>>();
                services.AddScoped<IMessageHandlerAsync<ClassificationFeedback>, ProcessClassificationFeedback>();

                // email service
                services.Configure<SmtpSettings>(options =>
                {
                    options.Server = hostContext.Configuration.GetValue<string>("AppSettings:SmtpHost");
                    options.Port = hostContext.Configuration.GetValue<int>("SmtpPort");
                    options.FromAddress = hostContext.Configuration.GetValue<string>("SmtpFromAddress");
                });
                services.AddTransient<IEmailService, EmailService>();

                services.AddHttpClient<IStemmerService, StemmerService>(config =>
                {
                    config.BaseAddress =
                        new Uri(
                            hostContext.Configuration.GetValue<string>("AppSettings:Classification.Service.Url"));
                });

                services.AddHttpClient<IHlidacService, HlidacService>(config =>
                {
                    config.BaseAddress = new Uri(hostContext.Configuration.GetValue<string>("AppSettings:APIUrl"));
                    config.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Token",
                            hostContext.Configuration.GetValue<string>("AppSettings:ApiToken"));
                });
            });
    }
}