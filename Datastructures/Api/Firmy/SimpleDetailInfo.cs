using System;
using System.ComponentModel;

namespace HlidacStatu.DS.Api.Firmy
{
    [Description("Simple detail information about legal entity, such as company or government office.")]
    public class SimpleDetailInfo
    {
        [Description("Unique identifier of the legal entity.")]
        public string Ico { get; set; }

        [Description("Name of the legal entity.")]
        public string Jmeno { get; set; }

        [Description("Region of the legal entity, if applicable. This is often used for government offices or municipalities.")]
        public string Kraj { get; set; } = null;

        [Description("URL to the source data of the legal entity. This is required to write when using the data.")]
        public string ZdrojUrl { get; set; } = "https://www.hlidacstatu.cz";
        
        [Description("Copyright notice for the data.")]
        public string Copyright { get; set; } = $"(c) {DateTime.Now.Year} Hlídač Státu z.ú.";

    }
}