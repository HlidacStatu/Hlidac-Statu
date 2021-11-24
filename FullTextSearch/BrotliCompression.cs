using System.IO;
using System.IO.Compression;

namespace FullTextSearch
{
    public class BrotliCompression
    {
        public static byte[] Compress(byte[] bytes)
        {
            using var outputStream = new MemoryStream();
            using var compressStream = new BrotliStream(outputStream, CompressionLevel.Fastest);
            compressStream.Write(bytes, 0, bytes.Length);
            compressStream.Flush();

            return outputStream.ToArray();
        }
        
        public static byte[] Decompress(byte[] bytes)
        {
            using var inputStream = new MemoryStream(bytes);
            using var outputStream = new MemoryStream();
            using var decompressStream = new BrotliStream(inputStream, CompressionMode.Decompress);

            decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}