using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Whisperer;

internal class DefaultAutocompleteAnalyzer : Analyzer
{
    private LuceneVersion _version;
    public DefaultAutocompleteAnalyzer(LuceneVersion version)
    {
        _version = version;
    }
    
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        Tokenizer tokenizer = new StandardTokenizer(_version, reader);
        TokenStream result = new StandardFilter(_version, tokenizer);
        result = new LowerCaseFilter(_version, result);
        result = new ASCIIFoldingFilter(result);
        //result = new StopFilter(result, ENGLISH_STOP_WORDS); // Můžeme nadefinovat vlastní stopwords
        result = new EdgeNGramTokenFilter(_version, result, 1, 30); // vytvoří index po písmenkách => není potřeba wildcard

        return new TokenStreamComponents(tokenizer, result);
    }
}

/// <summary>
/// We do not want to split query to EdgeNGrams
/// </summary>
internal class DefaultAutocompleteQueryAnalyzer : Analyzer
{
    private LuceneVersion _version;
    public DefaultAutocompleteQueryAnalyzer(LuceneVersion version)
    {
        _version = version;
    }
    
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        Tokenizer tokenizer = new StandardTokenizer(_version, reader);
        TokenStream result = new StandardFilter(_version, tokenizer);
        result = new LowerCaseFilter(_version, result);
        result = new ASCIIFoldingFilter(result);
       
        return new TokenStreamComponents(tokenizer, result);
    }
}