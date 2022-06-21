# How to use FullTextSearch

1) Create an `IEquatable` class you want to use as searchable object and add the `[FullTextSearch.Search]` attribute to those properties which are going to be searched 

```csharp
public class StatniWebyAutocomplete : IEquatable<StatniWebyAutocomplete>
{
    public int Id { get; set; } // This property won't be searched
    [FullTextSearch.Search] // This property will be searched
    public string Name { get; set; }
    [FullTextSearch.Search]
    public string Description { get; set; }
    [FullTextSearch.Search]
    public string Ico { get; set; }
    [FullTextSearch.Search]
    public string Url { get; set; }
    public string HostDomain { get; set; }

    public StatniWebyAutocomplete(UptimeServer uptimeServer)
    {
        Id = uptimeServer.Id;
        Name = uptimeServer.Name;
        Description = uptimeServer.Description;
        Ico = uptimeServer.ICO;
        Url = uptimeServer.PublicUrl;
        HostDomain = uptimeServer.HostDomain();

    }
    
    public bool Equals(StatniWebyAutocomplete other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StatniWebyAutocomplete)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }


}
```

2) Create Index for your objects
```csharp
var servers = UptimeServerRepo.AllActiveServers()
                .Select(uptimeServer => new StatniWebyAutocomplete(uptimeServer))
                .ToList();

var autocompleteIndex = new FullTextSearch.Index<StatniWebyAutocomplete>(servers);
```

3) Search happilly forever ever after

```csharp
var results = autocompleteIndex.Search("query", 10);
return results.Select(r => r.Original);
```