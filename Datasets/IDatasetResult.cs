namespace HlidacStatu.Datasets;

public interface IDatasetResult
{
    public System.Collections.Concurrent.ConcurrentBag<DataSearchResult> Results { get; set; }
}