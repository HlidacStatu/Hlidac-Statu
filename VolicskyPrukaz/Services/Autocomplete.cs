using System.IO.Compression;
using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories;
using Timer = System.Timers.Timer;

namespace VolicskyPrukaz.Services;

public class Autocomplete : IDisposable
{

    private static TimeSpan _expiration = TimeSpan.FromDays(7);
    private const string BaseDir = @"./Indexes/adresy/";
    private readonly Timer _timer;
    private bool _firstDir = true;

    private Whisperer.Index<AdresyKVolbam> _index;
    

    private void RenewCache()
    {
        try
        {
            Console.WriteLine($"Renew started at {DateTime.Now:HH:mm:ss zz}");
            // prepare other directory
            string otherDirectory = OtherDirectory();
            var newIndex = CreateIndex(otherDirectory);
            _index = newIndex;

            // we do not cleanup current directory now, so we do not have to solve concurrency issues
            // (can't delete index while any search is in progress)
            // so we leave cleanup to next folder switch (RenewCache call)

            _firstDir = !_firstDir; //switching directories
            Console.WriteLine("Renew finished");
        }
        catch (Exception e)
        {
            Console.WriteLine("Couldnt renew cache from repository");
            throw;
        }
    }
    

    public Autocomplete()
    {
        _index = CreateIndex(CurrentDirectory(), fromFile:true);
        // load from file
        
        // set timer for cache renewal
        _timer = new Timer(_expiration.TotalMilliseconds)
        {
            Enabled = true,
            AutoReset = true,
        };
        _timer.Elapsed += (_, _) => RenewCache();
        _timer.Start();

    }

    public IEnumerable<AdresyKVolbam> Search(string query)
    {
        return _index.Search(query);
    }
    

    private void DirectoryCleanup(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }

    private string CurrentDirectory()
    {
        return $"{BaseDir + _firstDir.ToString()}";
    }
    private string OtherDirectory()
    {
        return $"{BaseDir + (!_firstDir).ToString()}";
    }

    private Whisperer.Index<AdresyKVolbam> CreateIndex(string directory, bool fromFile = false)
    {
        DirectoryCleanup(directory);
        var index = new Whisperer.Index<AdresyKVolbam>(directory);
        List<AdresyKVolbam> adresy;
        if (fromFile)
        {
            var file = File.ReadAllBytes(@"./adresy.brotli");
            using var inputStream = new MemoryStream(file);
            using var outputStream = new MemoryStream();
            using var decompressStream = new BrotliStream(inputStream, CompressionMode.Decompress);
            decompressStream.CopyTo(outputStream);
            adresy = System.Text.Json.JsonSerializer.Deserialize<List<AdresyKVolbam>>(outputStream.ToArray());
        }
        else
        {
            adresy = AdresyRepo.GetAdresyKVolbamAsync().GetAwaiter().GetResult();
        }
            

        string[] boostedCities = new[] { "praha", "brno", "ostrava", "plzeň" };
        
        index.AddDocuments(adresy,
            addr => addr.Adresa,
            addr =>
            {
                if (boostedCities.Any(bc => addr.Obec.Contains(bc, StringComparison.InvariantCultureIgnoreCase)))
                    return 2.5f; //double boost
                // Typ Ovm = 5 .. 8
                return ((addr.TypOvm - 4) / 5f) + 1f;
            });
        return index;
    }
    

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _index?.Dispose();
    }
}