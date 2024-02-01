using System;
using System.Linq;

namespace HlidacStatu.Entities.Entities;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PU_Organizace")]
public class PuOrganizace
{
    public const char PathSplittingChar = '>';
    
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
    public virtual ICollection<PuOrganizaceMetadata> Metadata { get; set; }

    
    public KeyValuePair<string, string>[] OblastPath() => PathSplitter(Oblast);
    public KeyValuePair<string, string>[] ZatrideniPath() => PathSplitter(Oblast);

    public static string PathName(string path) => path.Split(PathSplittingChar).LastOrDefault();
    
    
    /// <returns>Key == nazev, Value == cesta</returns>
    public static KeyValuePair<string, string>[] PathSplitter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<KeyValuePair<string, string>>();
        
        var splitted = input.Split(PathSplittingChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        List<KeyValuePair<string, string>> paths = new();
        
        string currentPath = string.Empty;
        foreach (var split in splitted)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                currentPath = split;
            }
            else
            {
                currentPath = $"{currentPath}{PathSplittingChar}{split}";
            }
            paths.Add(new KeyValuePair<string, string>(split,currentPath));
        }

        return paths.ToArray();
    }
    
    
}