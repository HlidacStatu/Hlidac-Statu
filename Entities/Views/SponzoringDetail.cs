using System;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HlidacStatu.Entities.Views;

[Keyless]
public class SponzoringDetail
{
    public string NameIdDarce { get; set; }
    public string JmenoDarce { get; set; }
    public string PrijmeniDarce { get; set; }
    public DateTime? DaumNarozeniDarce { get; set; }

    public string IcoDarce { get; set; }
    public string IcoPrijemce { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Sponzoring.TypDaru TypDaru { get; set; }
    public decimal? HodnotaDaru { get; set; }
    public string Popis { get; set; }
    public DateTime? DarovanoDne { get; set; }
}