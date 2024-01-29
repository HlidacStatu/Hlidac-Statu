using System;
using System.Linq;

namespace HlidacStatu.Entities.Entities;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PU_Organizace")]
public class PuOrganizace
{
    private const char PathSplittingChar = '>';
    
    [Key]
    public int Id { get; set; }

    public string Ico { get; set; }
    public string DS { get; set; }
    public string Nazev { get; set; }
    public string Info { get; set; }
    public string HiddenNote { get; set; }
    public string Zatrideni { get; set; }
    public string Oblast { get; set; }

    // Navigation properties
    public virtual ICollection<PuOrganizaceTag> Tags { get; set; }
    public virtual ICollection<PuPlat> Platy { get; set; }
    public virtual ICollection<PuOranizaceMetadata> Metadata { get; set; }

    public string[] OblastPath() => PathSplitter(Oblast);
    public string[] ZatrideniPath() => PathSplitter(Oblast);

    public string PathName(string path) => path.Split(PathSplittingChar).LastOrDefault();

    private string[] PathSplitter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();
        
        var splitted = input.Split(PathSplittingChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        List<string> result = new List<string>();
        string current = string.Empty;
        foreach (var split in splitted)
        {
            current = $"{current}{PathSplittingChar}{split}";
            result.Add(current);
        }

        return result.ToArray();
    }
}