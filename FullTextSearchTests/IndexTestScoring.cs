using System.Collections.Generic;
using System.Linq;
using FullTextSearch;
using Xunit;

namespace FullTextSearchTests;

public class IndexTestScoring
{
    private Index<MyIndexedClass> _index { get; }
    
    public IndexTestScoring()
    {
        var items = new List<MyIndexedClass>()
        {
            new() { Id = 1, IndexedText = "abraka dabra", Unindexed = "první" },
            new() { Id = 2, IndexedText = "dabra abraka", Unindexed = "prvni" },
            new() { Id = 3, IndexedText = "andrej andrejovic", Unindexed = "druhy" },
            new() { Id = 4, IndexedText = "andrejovic andrej", Unindexed = "druhy" },
            new() { Id = 5, IndexedText = "andrej", Unindexed = "druhy" },
            new() { Id = 6, IndexedText = "andrejovic", Unindexed = "druhy" },
            new() { Id = 7, IndexedText = "Andrejka Nováková", Unindexed = "druhy" },
            new() { Id = 8, IndexedText = "pracka tlacka macka", Unindexed = "treti" },
            new() { Id = 9, IndexedText = "pracka macka tlacka", Unindexed = "treti" },
            new() { Id = 10, IndexedText = "tlacka macka pracka", Unindexed = "treti" },
            new() { Id = 11, IndexedText = "tlacka pracka macka placka", Unindexed = "treti" },
            new() { Id = 12, IndexedText = "pracka macka placka tlacka", Unindexed = "treti" },

        };

        _index = new Index<MyIndexedClass>(items);
    }
    
    [Fact]
    public void ReturnsShorterNamesFirst()
    {
        var results = _index.Search("andrej", 5).ToList();
        Assert.Equal(5, results.Count);
        Assert.Equal(5, results[0].Original.Id);
        Assert.Equal(3, results[1].Original.Id);
        Assert.Equal(4, results[2].Original.Id);
        Assert.Equal(6, results[3].Original.Id);
        Assert.Equal(7, results[4].Original.Id);
    }
    
    [Fact]
    public void CheckThatBonusForFirstWordsWorks()
    {
        var results = _index.Search("abr dab", 5).ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Original.Id);
        Assert.Equal(2, results[1].Original.Id);
    }
    
    [Fact]
    public void CheckThatBonusForFirstWordsWorks2()
    {
        var results = _index.Search("dab abr", 5).ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(2, results[0].Original.Id);
        Assert.Equal(1, results[1].Original.Id);
    }
    
    [Fact]
    public void CheckLongestChainBonusFn()
    {
        var results = _index.Search("pra tla ma", 5).ToList();
        Assert.Equal(5, results.Count);
        Assert.Equal(8, results[0].Original.Id);
    }
    
    [Fact]
    public void CheckLongestChainBonusFn2()
    {
        var results = _index.Search("pra ma tla pl", 5).ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(12, results[0].Original.Id);
        Assert.Equal(11, results[1].Original.Id);
    }
    
    
}