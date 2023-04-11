namespace Whisperer;

public record ScoredResult<T>(float Score, T Document) : IEquatable<ScoredResult<T>> where T : IEquatable<T>
{

    public virtual bool Equals(ScoredResult<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Document.Equals(other.Document);
    }

    public override int GetHashCode()
    {
        return Document.GetHashCode();
    }
}