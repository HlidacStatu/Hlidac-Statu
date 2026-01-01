using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Util;
public static class GzipArchiveExtractor
{
    private static readonly byte[] GzipMagicBytes = [0x1F, 0x8B];
    private static readonly byte[] TarMagicBytes = "ustar"u8.ToArray(); // TAR magic na pozici 257

    /// <summary>
    /// Stáhne GZIP soubor z URL a rozbalí obsah do cílového adresáře.
    /// Podporuje jak čistý GZIP (jeden soubor), tak TAR.GZ (více souborů).
    /// </summary>
    public static async Task<IReadOnlyList<string>> DownloadAndExtractAsync(
        string url,
        string destinationDirectory,
        bool overwriteIfDifferent = false,
            TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        using var httpClient = new HttpClient();
        var archiveData = await httpClient.GetByteArrayAsync(url, cancellationToken);

        if (!IsGzipArchive(archiveData))
        {
            throw new InvalidDataException(
                $"Stažený soubor není platný GZIP archiv. " +
                $"Očekávány magic bytes: {BitConverter.ToString(GzipMagicBytes)}, " +
                $"nalezeny: {BitConverter.ToString(archiveData.Take(2).ToArray())}");
        }

        Directory.CreateDirectory(destinationDirectory);

        // Nejprve dekomprimujeme GZIP
        var decompressedData = await DecompressGzipAsync(archiveData, cancellationToken);

        // Zjistíme, zda je to TAR archiv
        if (IsTarArchive(decompressedData))
        {
            return await ExtractTarAsync(
                decompressedData,
                destinationDirectory,
                overwriteIfDifferent,
                cancellationToken);
        }
        else
        {
            // Čistý GZIP - jeden soubor
            return await ExtractSingleFileAsync(
                decompressedData,
                url,
                destinationDirectory,
                overwriteIfDifferent,
                cancellationToken);
        }
    }

    /// <summary>
    /// Verze s progress reportingem.
    /// </summary>
    public static async Task<IReadOnlyList<string>> DownloadAndExtractWithProgressAsync(
        string url,
        string destinationDirectory,
        IProgress<DownloadProgress>? downloadProgress = null,
        IProgress<ExtractionProgress>? extractionProgress = null,
        TimeSpan? timeout = null,
        bool overwriteIfDifferent = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        using var httpClient = new HttpClient();

        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();

        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            downloadProgress?.Report(new DownloadProgress(
                downloadedBytes,
                totalBytes,
                totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : -1));
        }

        var archiveData = memoryStream.ToArray();

        if (!IsGzipArchive(archiveData))
        {
            throw new InvalidDataException("Stažený soubor není platný GZIP archiv.");
        }

        Directory.CreateDirectory(destinationDirectory);

        var decompressedData = await DecompressGzipAsync(archiveData, cancellationToken);

        if (IsTarArchive(decompressedData))
        {
            return await ExtractTarAsync(
                decompressedData,
                destinationDirectory,
                overwriteIfDifferent,
                cancellationToken,
                extractionProgress);
        }
        else
        {
            return await ExtractSingleFileAsync(
                decompressedData,
                url,
                destinationDirectory,
                overwriteIfDifferent,
                cancellationToken);
        }
    }

    private static bool IsGzipArchive(byte[] data)
    {
        return data.Length >= 2
            && data[0] == GzipMagicBytes[0]
            && data[1] == GzipMagicBytes[1];
    }

    private static bool IsTarArchive(byte[] data)
    {
        // TAR magic "ustar" je na pozici 257
        if (data.Length < 262)
            return false;

        var magicSpan = data.AsSpan(257, 5);
        return magicSpan.SequenceEqual(TarMagicBytes);
    }

    private static async Task<byte[]> DecompressGzipAsync(
        byte[] gzipData,
        CancellationToken cancellationToken)
    {
        await using var compressedStream = new MemoryStream(gzipData);
        await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();

        await gzipStream.CopyToAsync(decompressedStream, cancellationToken);

        return decompressedStream.ToArray();
    }

    private static async Task<IReadOnlyList<string>> ExtractTarAsync(
        byte[] tarData,
        string destinationDirectory,
        bool overwriteIfDifferent,
        CancellationToken cancellationToken,
        IProgress<ExtractionProgress>? progress = null)
    {
        var extractedFiles = new List<string>();
        var destinationFullPath = Path.GetFullPath(destinationDirectory);

        await using var tarStream = new MemoryStream(tarData);
        using var tarReader = new TarReader(tarStream);

        var entryIndex = 0;

        while (await tarReader.GetNextEntryAsync(copyData: true, cancellationToken) is { } entry)
        {
            entryIndex++;

            // Přeskočit adresáře a speciální typy
            if (entry.EntryType is TarEntryType.Directory
                or TarEntryType.GlobalExtendedAttributes
                or TarEntryType.ExtendedAttributes)
            {
                continue;
            }

            // Pouze soubory
            if (entry.EntryType is not (TarEntryType.RegularFile
                or TarEntryType.V7RegularFile
                or TarEntryType.ContiguousFile))
            {
                continue;
            }

            // Bezpečnostní kontrola - prevence path traversal
            var entryPath = SanitizeEntryPath(entry.Name);
            var fullPath = Path.GetFullPath(Path.Combine(destinationDirectory, entryPath));

            if (!fullPath.StartsWith(destinationFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Pokus o path traversal attack detekován: {entry.Name}");
            }

            // Vytvoření adresářové struktury
            var fileDirectory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            // Kontrola existence souboru
            // Pokud nepřepisujeme, najdeme unikátní název
            if (File.Exists(fullPath))
            {
                if (overwriteIfDifferent)
                {

                    var isDiff = Devmasters.IO.BinaryComparer.AreEqual(fullPath, entry.DataStream);
                    if (isDiff)
                    {
                        await entry.ExtractToFileAsync(fullPath, overwriteIfDifferent, cancellationToken);
                        extractedFiles.Add(fullPath);
                    }
                }
            }
            else
            {
                await entry.ExtractToFileAsync(fullPath, overwriteIfDifferent, cancellationToken);
                extractedFiles.Add(fullPath);
            }
            progress?.Report(new ExtractionProgress(entryIndex, entry.Name, fullPath));
        }

        return extractedFiles;
    }

    private static async Task<IReadOnlyList<string>> ExtractSingleFileAsync(
        byte[] decompressedData,
        string originalUrl,
        string destinationDirectory,
        bool overwriteIfDifferent,
        CancellationToken cancellationToken)
    {
        // Odvodíme název souboru z URL (odstraníme .gz příponu)
        var fileName = GetFileNameFromUrl(originalUrl);
        var fullPath = Path.Combine(destinationDirectory, fileName);

        if (File.Exists(fullPath))
        {
            if (overwriteIfDifferent)
            {
                var isDiff = Devmasters.IO.BinaryComparer.AreEqual(fullPath, decompressedData);
                if (isDiff)
                {
                    await File.WriteAllBytesAsync(fullPath, decompressedData, cancellationToken);
                    return [fullPath];
                }
            }

        }
        else
        {
            await File.WriteAllBytesAsync(fullPath, decompressedData, cancellationToken);
            return [fullPath];
        }
        return Array.Empty<string>();
    }

    private static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        var fileName = Path.GetFileName(uri.LocalPath);

        // Odstranění .gz přípony
        if (fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[..^3];
        }

        // Pokud je to .tar.gz, bude mít teď .tar, což je správně pro TAR
        // Pro čistý .gz bez dalšího jména použijeme default
        if (string.IsNullOrWhiteSpace(fileName) || fileName == ".tar")
        {
            fileName = "extracted_file";
        }

        return SanitizeEntryPath(fileName);
    }

    private static string SanitizeEntryPath(string entryName)
    {
        var sanitized = entryName
            .Replace('\\', '/')
            .TrimStart('/')
            .Replace("../", string.Empty)
            .Replace("./", string.Empty);

        var segments = sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var safeSegments = segments.Where(s => s != ".." && s != ".").ToArray();

        return Path.Combine(safeSegments);
    }


    public readonly record struct DownloadProgress(
        long BytesDownloaded,
        long TotalBytes,
        double PercentComplete);

    public readonly record struct ExtractionProgress(
        int EntryIndex,
        string EntryName,
        string ExtractedPath);

}