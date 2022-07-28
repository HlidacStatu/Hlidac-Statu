using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;

namespace AsrRunner;

public class TaskQueueService : IDisposable
{
    private ILogger _logger;
   
    private AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(Global.HttpRetryCount, attempt => TimeSpan.FromMilliseconds(Global.HttpRetryCount));
    
    private JsonSerializerOptions? _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient _httpClient = new();
    private string _currentQueueItem = string.Empty;

    public TaskQueueService(ILogger logger)
    {
        _logger = logger.ForContext<TaskQueueService>();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Global.ApiKey);
    }

    public async Task<QueueItem?> GetNewTaskAsync(CancellationToken cancellationToken)
    {
        using var responseMessage = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.GetAsync(Global.TaskQueueUrl, cancellationToken));

        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.Error("Error during {methodName}. Server responded with [{statusCode}] status code. Reason phrase [{reasonPhrase}].",
                nameof(GetNewTaskAsync), responseMessage.StatusCode, responseMessage.ReasonPhrase);
            return null;
        }
        
        _currentQueueItem = await responseMessage.Content.ReadAsStringAsync();
        CurrentQueueItemCheck();
        
        return JsonSerializer.Deserialize<QueueItem>(_currentQueueItem, _jsonSerializerOptions);
    }


    public async Task ReportSuccessAsync(CancellationToken cancellationToken)
    {
        CurrentQueueItemCheck();
        
        var data = new StringContent(_currentQueueItem, Encoding.UTF8, "application/json");
        using var responseMessage = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(Global.ReportSuccessUrl, data, cancellationToken));

        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.Error("Error during {methodName}. Server responded with [{statusCode}] status code. Reason phrase [{reasonPhrase}].",
                nameof(ReportSuccessAsync), responseMessage.StatusCode, responseMessage.ReasonPhrase);
            
            throw new HttpRequestException(
                $"Request error during {nameof(ReportSuccessAsync)}. Server responded with [{responseMessage.ReasonPhrase}] reason.",
                null, responseMessage.StatusCode);
        }
    }
    
    public async Task ReportFailureAsync(CancellationToken cancellationToken)
    {
        CurrentQueueItemCheck();
        
        var data = new StringContent(_currentQueueItem, Encoding.UTF8, "application/json");
        using var responseMessage = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(Global.ReportFailureUrl, data, cancellationToken));

        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.Error("Error during {methodName}. Server responded with [{statusCode}] status code. Reason phrase [{reasonPhrase}].",
                nameof(ReportSuccessAsync), responseMessage.StatusCode, responseMessage.ReasonPhrase);
            
            throw new HttpRequestException(
                $"Request error during {nameof(ReportFailureAsync)}. Server responded with [{responseMessage.ReasonPhrase}] reason.",
                null, responseMessage.StatusCode);
        }
    }
    
    private void CurrentQueueItemCheck()
    {
        if (string.IsNullOrWhiteSpace(_currentQueueItem))
        {
            _logger.Error("Missing or empty {propertyName}.", nameof(_currentQueueItem));
            throw new ArgumentNullException(nameof(_currentQueueItem));
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}