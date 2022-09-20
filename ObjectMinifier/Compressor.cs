using System.IO.Compression;
using System.Text.Json;

namespace ObjectMinifier;

public static class Compressor
{
    public static void CompressToFile<T>(T data, string path)
    {
        var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(data);
        var compressedData = Compress(utf8Bytes);
        File.WriteAllBytes(path, compressedData);
    }
    
    public static T ReadFromFile<T>(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var decompressed = Decompress(bytes);
        var deserialized = JsonSerializer.Deserialize<T>(decompressed);
        return deserialized;
    }
    
    public static byte[] CompressToByteArr<T>(T data)
    {
        var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(data);
        return Compress(utf8Bytes);
    }
    
    public static T ReadFromByteArr<T>(byte[] bytes)
    {
        var decompressed = Decompress(bytes);
        var deserialized = JsonSerializer.Deserialize<T>(decompressed);
        return deserialized;
    }
    
    private static byte[] Compress(byte[] bytes)
    {
        using var outputStream = new MemoryStream();
        using var compressStream = new BrotliStream(outputStream, CompressionLevel.Fastest);
        compressStream.Write(bytes, 0, bytes.Length);
        compressStream.Flush();

        return outputStream.ToArray();
    }

    private static byte[] Decompress(byte[] bytes)
    {
        using var inputStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();
        using var decompressStream = new BrotliStream(inputStream, CompressionMode.Decompress);

        decompressStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}