using Nest;

using System;
namespace HlidacStatu.Entities.Logs
{
    public class ProfilZadavateleDownload : ILogs
    {
        public DateTime Date { get; set; }

        [Keyword]
        public string ProfileId { get; set; }

        public long ResponseMs { get; set; }

        [Boolean]
        public bool? HttpValid { get; set; } = null;


        public string HttpError { get; set; }
        public int? HttpErrorCode { get; set; } = null;

        public bool? XmlValid { get; set; } = null;
        public string XmlError { get; set; }
        public string XmlInvalidContent { get; set; }

        [Keyword]
        public string RequestedUrl { get; set; }

    }

}
