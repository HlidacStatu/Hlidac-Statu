using System;
using Nest;

namespace HlidacStatu.Entities;

/// <summary>
/// Slouží pro ukládání historie dokumentů
/// </summary>
public class DocumentHistory
{
    [Date]
    public DateTime SaveDate { get; set; } = DateTime.Now;
    
    [Keyword]
    public string DataSource { get; set; }
    

    [Keyword]
    public string DocumentId { get; set; }
    
    [Object]
    public Object Document { get; set; }
    [Keyword]
    public string DocumentHash { get; set; }
    [Keyword]
    public string DocumentType { get; set; }

    
}