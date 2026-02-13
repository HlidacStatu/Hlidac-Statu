using System;
using System.Collections.Generic;
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

    public class CheckSumIgnoreAttribute : Attribute
    {
    }

    static string NormalizeValue(object? value)
    {
        if (value is null) return "";

        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? "1" : "0",
            decimal m => m.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString("G9", System.Globalization.CultureInfo.InvariantCulture),
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }

    static string Canonicalize(string s)
        => s.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", " ");

    static Type[] _defaultAtributesToIgnore = new Type[] { 
        typeof(CheckSumIgnoreAttribute),
        typeof(System.Runtime.Serialization.IgnoreDataMemberAttribute),
        typeof(System.Text.Json.Serialization.JsonIgnoreAttribute),
        typeof(Newtonsoft.Json.JsonIgnoreAttribute),
        typeof(System.Xml.Serialization.XmlIgnoreAttribute),

    };

    private static bool ShouldIgnoreProperty(System.Reflection.PropertyInfo prop, Type[] attributesToIgnore)
    {
        foreach (var attrType in attributesToIgnore)
        {
            if (prop.GetCustomAttributes(attrType, inherit: true).Any())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="ignoreProperties"></param>
    /// <param name="skipAttributesCheck"></param>
    /// <param name="customAttributesToIgnore"></param>
    /// <returns></returns>
    public static string ObjectCheckSum(object obj, string[] ignoreProperties = null, 
        bool skipAttributesCheck = false,
        Type[] customAttributesToIgnore = null
        )
    {
        ignoreProperties = ignoreProperties ?? Array.Empty<string>();

        var _actualAttributesToIgnore = Array.Empty<Type>();   
        if (skipAttributesCheck == false)
        {
            _actualAttributesToIgnore = customAttributesToIgnore != null ? customAttributesToIgnore : _defaultAtributesToIgnore;
        }

        List<(string Name, string? Value, bool ignore)> propValues  = obj.GetType()
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(p => new
            {
                Prop = p,
                ignore = ShouldIgnoreProperty(p, _actualAttributesToIgnore) || ignoreProperties.Contains(p.Name)
            })
            .OrderBy(x => x.Prop.Name, System.StringComparer.Ordinal)
            .Select(x => (
                Name: x.Prop.Name, 
                Value: x.ignore? null : Canonicalize(NormalizeValue(x.Prop.GetValue(obj))), 
                Ignore: x.ignore
                ))
            .ToList()
            ;


        var str = string.Join("\u001F", propValues.Where(p=>p.ignore==false).Select(p => $"{p.Name}={p.Value}")); // unit-separator
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash); // uppercase hex
    }

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