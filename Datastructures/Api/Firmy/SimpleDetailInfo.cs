using HlidacStatu.DS.Api.MCP;
using System;
using System.ComponentModel;

namespace HlidacStatu.DS.Api.Firmy
{
    [Description("Simple detail information about legal entity, such as company or government office.")]
    public class SimpleDetailInfo : MCPBaseResponse
    {
        [Description("Unique identifier of the legal entity.")]
        public string Ico { get; set; }

        [Description("Name of the legal entity.")]
        public string Jmeno { get; set; }

        [Description("Region of the legal entity, if applicable. This is often used for government offices or municipalities.")]
        public string Kraj { get; set; } = null;


    }
}