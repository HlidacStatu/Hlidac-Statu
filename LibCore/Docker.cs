using System;
using System.Net.Http;
using System.Threading.Tasks;

using Polly;


namespace HlidacStatu.LibCore;

public static class Docker
{
    
    /// <summary>
    /// This method waits until another service on "serviceUrlCheckEndpoint" starts responding.
    /// It sends HTTP GET requests in 1 sec intervals and waits until it gets 2xx HTTP response. 
    /// </summary>
    /// <param name="serviceUrlCheckEndpoint"></param>
    public static async Task WaitUntilServiceIsRunning(string serviceUrlCheckEndpoint)
    {
        int retries = 0;

        using var httpClient = new HttpClient();
        
        var policy = Policy.Handle<Exception>().WaitAndRetryForeverAsync(
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(1), // Wait 1s between each try.
            onRetry: (exception, calculatedWaitDuration) => // Capture some info for logging!
            {
                Console.WriteLine($"Attempt #{retries++} to connect with {serviceUrlCheckEndpoint} was unsuccessful. {exception.Message}");
            });

        await policy.ExecuteAsync(async () =>
        {
            using var result = await httpClient.GetAsync(serviceUrlCheckEndpoint);
            _ = result.EnsureSuccessStatusCode();
        });
    }
}