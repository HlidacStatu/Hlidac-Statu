using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;


namespace HlidacStatu.Util;

public class Checksum
{
    private const string InvalidChecksum = "Invalid_";
    private static readonly ILogger _logger = Log.ForContext(typeof(Checksum));

    private static AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.Forbidden)
        .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(1 * attempt));
    
    public static async Task<string> ChecksumFromUrlAsync(HttpClient httpClient, string url) 
    {
        using var responseMessage = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));

        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.Warning($"Couldn't get {url}");
            return default;
        }

        try
        {
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var result = await DoChecksumAsync(stream);
            return result;
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Url: {url} can not process stream properly.");
            throw;
        }
    }

    public static string GenerateInvalidChecksum()
    {
        return InvalidChecksum + Guid.NewGuid().ToString("N");
    }

    public static string DoChecksum(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return ConvertToCorrectEndiannessFormat(hash);
    }
    
    public static string DoChecksum(byte[] file)
    {
        if (file is null || file.Length == 0)
            return GenerateInvalidChecksum();

        var hash = SHA256.HashData(file);
        return ConvertToCorrectEndiannessFormat(hash);
    }

    public static async Task<string> DoChecksumAsync(Stream stream)
    {
        if (stream is null)
        {
            return GenerateInvalidChecksum();
        }

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return ConvertToCorrectEndiannessFormat(hash);

    }

    private static string ConvertToCorrectEndiannessFormat(byte[] hash)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.ToString(hash).Replace("-", "");
        }
        else
        {
            return BitConverter.ToString(hash.Reverse().ToArray()).Replace("-", "");
        }
    }
    
}