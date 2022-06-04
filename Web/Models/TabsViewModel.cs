using System.Collections.Generic;

namespace HlidacStatu.Web.Models
{
    public class TabsViewModel
    {
        public IEnumerable<string>? Tabnames { get; set; }
        public IEnumerable<string>? Contents { get; set; }
        public string ContainerCss { get; set; }
        public string TabColCss { get; set; }
        public string ContentColCss { get; set; }
        public string? GaPageEventId { get; set; }

        public TabsViewModel(IEnumerable<string>? tabnames,
            IEnumerable<string>? contents,
            string containerCss = "col-xs-12",
            string tabColCss = "col-lg-2 col-md-2 col-sm-2 col-xs-3",
            string contentColCss = "col-lg-10 col-md-10 col-sm-10 col-xs-9",
            string? gaPageEventId = null)
        {
            Tabnames = tabnames;
            Contents = contents;
            ContainerCss = containerCss;
            TabColCss = tabColCss;
            ContentColCss = contentColCss;
            GaPageEventId = gaPageEventId;
        }
    }
}