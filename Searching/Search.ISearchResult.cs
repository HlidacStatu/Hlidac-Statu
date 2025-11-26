namespace HlidacStatu.Searching
{
    public partial class Search
    {

        public interface ISearchResult
        {
            long Total { get; }
            bool IsValid { get; }
            bool HasResult { get; }
            int PageSize { get; }
            int Page { get; }
            string Order { get; set; }
            string DataSource { get; set; }
            string Query { get; set; }

            int MaxResultWindow();

            RouteValues ToRouteValues(int page);

            TimeSpan ElapsedTime { get; set; }
        }
        
    }

}