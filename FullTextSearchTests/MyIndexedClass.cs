using System;

namespace FullTextSearchTests;

public class MyIndexedClass : IEquatable<MyIndexedClass>
{
    public int Id { get; init; }
    [FullTextSearch.Search]
    public string IndexedText { get; init; }
    [FullTextSearch.Search]
    public string AnotherIndexedText { get; init; }
    public string Unindexed { get; init; }
    
    public bool Equals(MyIndexedClass? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MyIndexedClass)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }
}