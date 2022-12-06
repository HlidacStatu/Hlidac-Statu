using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HlidacStatu.Util;

public class Checksum
{
    private const string InvalidChecksum = "nofilefound";

    public static string DoChecksum(byte[] file)
    {
        if (file is null || file.Length == 0)
            return InvalidChecksum;

        var hash = SHA256.HashData(file);
        return ConvertToCorrectEndiannessFormat(hash);
    }

    public static async Task<string> DoChecksumAsync(Stream stream)
    {
        if (stream is null)
        {
            return InvalidChecksum;
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