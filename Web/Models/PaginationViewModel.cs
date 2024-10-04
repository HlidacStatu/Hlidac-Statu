using HlidacStatu.Repositories.Searching;

using System.Collections.Generic;

namespace HlidacStatu.Web.Models
{
    public class PaginationViewModel
    {
        public HlidacStatu.Searching.Search.ISearchResult Result { get; set; }

        public string UriPath { get; set; }
        public string? ExportType { get; set; }
        public string ExportMoreParams { get; set; }

        public PaginationViewModel(HlidacStatu.Searching.Search.ISearchResult result,
            string uriPath,
            IDictionary<string, object>? htmlAttributes = null,
            string? exportType = null,
            string exportMoreParams = "")
        {
            Result = result;
            UriPath = uriPath;
            ExportType = exportType;
            ExportMoreParams = exportMoreParams;
        }
    }

}