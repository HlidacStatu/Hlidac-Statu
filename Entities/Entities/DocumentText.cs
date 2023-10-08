using System;
using Nest;

namespace HlidacStatu.Entities;

public class DocumentText
{
    [Keyword()]
    public string Checksum { get; set; }
    [Object(Enabled = false)]
    public string Text { get; set; }
    [Date]
    public DateTime LastUpdate { get; set; }
}