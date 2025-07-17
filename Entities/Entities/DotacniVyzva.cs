using System;
using System.Collections.Generic;
using System.Linq;
using Devmasters;
using Devmasters.Enums;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public partial class DotacniVyzva
{
    [Keyword]
    public string Id { get; set; }
    
    [Keyword]
    public string TheirId { get; set; }

    [Date]
    public DateTime? DateFrom { get; set; }

    [Date]
    public DateTime? DateTo { get; set; }

    [Date]
    public DateTime? ModifiedDate { get; set; }


    public string Name { get; set; }

    [Text]
    public List<string> Rules { get; set; }

    [Keyword]
    public List<string> UrlLinks { get; set; }

    [Keyword]
    public string ProgramCode { get; set; }

    public string ProgramName { get; set; }

    [Text]
    public List<string> Targets { get; set; }
    
    [Text]
    public List<string> Fonds { get; set; }

    [Number]
    public decimal? Allocation { get; set; }
    
    [Object(Enabled = false)] //do not index, do not process, just store
    public List<object> RawData { get; set; } = new();
    
    //Our enrichment
    public string Category { get; set; }
    [Keyword]
    public string DataSource { get; set; }
    [Keyword]
    public string FileName { get; set; }
    [Number]
    public int RecordNumber { get; set; }
    
}