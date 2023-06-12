using System.Text.Json;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace Whisperer;

public sealed class Index<T> : IDisposable where T : IEquatable<T>
{
    public const LuceneVersion LuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

    private readonly HashSet<char> SearchModifiingChars = new("&|+-!(){}[]^\"~*?:\\/".ToCharArray());
        
    private const string SearchFieldName = nameof(SearchFieldName);
    private const string DataFieldName = nameof(DataFieldName);
    private const string FilterFieldName = nameof(FilterFieldName);

    private readonly Analyzer _indexAnalyzer;
    private readonly Analyzer _queryAnalyzer;
    private readonly Directory _directory;

    /// <summary>
    /// Creates Whisperer's Index. 
    /// </summary>
    /// <param name="directoryPath">Path to a directory where all documents are saved. Every instance should have its own documents!</param>
    /// <param name="indexAnalyzer">If needed, you can create your custom Analyzer for storing data.</param>
    /// <param name="queryAnalyzer">If needed, you can create your custom Analyzer for processing query.</param>
    public Index(string directoryPath)
    {
        _directory = new MMapDirectory(directoryPath);
        _indexAnalyzer = new DefaultAutocompleteAnalyzer(LuceneVersion);
        _queryAnalyzer = new DefaultAutocompleteQueryAnalyzer(LuceneVersion);
    }

    /// <summary>
    /// Adds documents, and creates indexes
    /// </summary>
    /// <param name="documents">Objects which are stored</param>
    /// <param name="textSelector">Text, which is going to be indexed and searched.</param>
    /// <param name="boostSelector">Multiplier - how much is the document going to be boosted.</param>
    /// <param name="filterSelector">If you want to use filtered search, then you can create your own filters.
    /// Reccommendation is to use simple lowercase words without diacritics (accents)</param>
    public void AddDocuments(IEnumerable<T> documents,
        Func<T, string> textSelector,
        Func<T, float>? boostSelector = null,
        Func<T, string>? filterSelector = null)
    {
        IndexWriterConfig config = new IndexWriterConfig(LuceneVersion, _indexAnalyzer);

        using IndexWriter iwriter = new IndexWriter(_directory, config);

        foreach (var document in documents)
        {
            Document doc = new Document();
            // store data
            var json = JsonSerializer.Serialize(document); //todo: try performance with bytearray
            doc.Add(new StoredField(DataFieldName, json));

            // store search field
            var searchField = new TextField(SearchFieldName, textSelector(document), Field.Store.YES);
            if (boostSelector is not null)
            {
                searchField.Boost = boostSelector(document);
            }

            doc.Add(searchField);

            if (filterSelector is not null)
            {
                string filter = filterSelector(document);
                doc.Add(new TextField(FilterFieldName, filter, Field.Store.YES));
            }

            iwriter.AddDocument(doc);
        }

        iwriter.Dispose();
    }

    /// <summary>
    /// Deletes all indexes from disk
    /// </summary>
    public void DeleteDocuments()
    {
        var files = _directory.ListAll();
        foreach (var file in files)
        {
            _directory.ClearLock(file);
            _directory.DeleteFile(file);
        }
    }

    public IEnumerable<ScoredResult<T>> Search(string query, int numResults = 10, string? filter = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<ScoredResult<T>>();
        
        
        
        // It can happen that in results will be synonyms which are going to be filtered out
        // so we need this "buffer"
        var maxResults = numResults * 5;

        using var directoryReader = DirectoryReader.Open(_directory);
        var indexSearcher = new IndexSearcher(directoryReader);
        var queryParser = new QueryParser(LuceneVersion, SearchFieldName, _queryAnalyzer)
        {
            DefaultOperator = Operator.AND
        };
        
        query = QueryParser.Escape(query) //we need to escape special characters
            .ToLower(); //also we need to remove boolean operators AND OR NOT - make them lowercase is enough
        
        // query = query.ReplaceDisallowedCharsWithSpace(SearchModifiingChars) //we need to remove special characters
        //     .ToLower(); //also we need to remove boolean operators AND OR NOT - make them lowercase is enough  

        var searchQuery = queryParser.Parse(query);

        var finalQuery = new BooleanQuery();
        finalQuery.Add(searchQuery, Occur.MUST);

        if (filter is not null)
        {
            var filterParser = new QueryParser(LuceneVersion, FilterFieldName, _queryAnalyzer)
            {
                DefaultOperator = Operator.OR
            };

            var filterQuery = filterParser.Parse(filter);
            finalQuery.Add(filterQuery, Occur.MUST);
        }

        var hits = indexSearcher.Search(finalQuery, null, maxResults);

        var scoredDocuments = hits.ScoreDocs
            .Select(doc => new ScoredResult<T>(
                doc.Score, 
                GetOriginalFromLuceneDocument(indexSearcher.Doc(doc.Doc))
            ));

        var results = new HashSet<ScoredResult<T>>(scoredDocuments);

        return results.Take(numResults);
    }

    private T GetOriginalFromLuceneDocument(Document luceneDocument)
    {
        var json = luceneDocument.GetField(DataFieldName).GetStringValue();
        return JsonSerializer.Deserialize<T>(json);
    }

    public void Dispose()
    {
        _indexAnalyzer?.Dispose();
        _queryAnalyzer?.Dispose();
        _directory.Dispose();
    }
}