namespace HlidacStatu.Searching
{
    public partial class Search
    {
        public class GeneralResult<T> : ISearchResult
        {
            public string Query { get; set; }
            public IEnumerable<T> Result { get; private set; }
            public long Total { get; private set; }
            public int PageSize { get; private set; }
            public int Page { get; set; }
            public string Order { get; set; }
            public bool IsValid { get; private set; }
            public bool HasResult { get { return IsValid && Total > 0; } }
            public string DataSource { get; set; }
            public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

            public void AppendResult(T item)
            {
                if (Result == null) 
                    Result = new List<T>();
                Result = Result.Append(item);
                IsValid= true;
                Total = Result.Count();
            }

            public virtual int MaxResultWindow() { return Tools.MaxResultWindow; }

            public GeneralResult(string query, IEnumerable<T> result, int pageSize)
                : this(query, result?.Count() ?? 0, result, pageSize, true)
            { }

            public GeneralResult(string query, long total, IEnumerable<T> result, int pageSize, bool isValid = true)
            {
                Query = query;
                Result = result;
                Total = total;
                IsValid = isValid;
                PageSize = PageSize;
            }


            public RouteValues ToRouteValues(int page)
            {
                return new()
                {
                    Q = Query,
                    Page = page
                };
            }

        }


    }
}
