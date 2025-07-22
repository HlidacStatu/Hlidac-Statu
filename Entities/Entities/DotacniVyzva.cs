using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Devmasters;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public partial class DotacniVyzva
{
    private string _id = null;
    [Keyword]
    public string Id
    {
        get
        {
            if (_id == null)
            {
                var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(
                    $"{Name}_{TheirId}"));
                var hash = Convert.ToHexString(hashBytes);
                _id = $"{DataSource.RemoveAccents().Replace(" ", "")}-" +
                      $"{FileName.RemoveAccents().Replace(" ", "").Replace(".", "")}-{hash}";
            }
            return _id;
        }
        set => _id = value;
    }
    
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
    public List<string> Rules { get; set; } = new();

    [Keyword]
    public List<string> UrlLinks { get; set; } = new();

    [Keyword]
    public string ProgramCode { get; set; }

    public string ProgramName { get; set; }

    [Text]
    public List<string> Targets { get; set; } = new();

    [Text]
    public HashSet<string> Fonds { get; set; } = new();

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