using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace Whisperer;

public class IndexingOptions<T> where T : IEquatable<T>
{
    public Func<T, string> TextSelector { get; init; }
    public Func<T, float>? BoostSelector { get; init; } = null;
    public Func<T, string>? FilterSelector { get; init; } = null;
    
    public Analyzer IndexAnalyzer { get; init; } = null;
    public Analyzer QueryAnalyzer { get; init; } = null;
}