using Devmasters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel
{

    public readonly record struct KategorieVozidlaInfo(string Kod, string Popis, KategorieVozidlaTyp Typ);
    public readonly record struct KategorieVozidlaTyp(string Jmeno, string Icon);
    public static class KategorieVozidla
    {
        public static KategorieVozidlaTyp GetTyp(string typName)
        {
            if (string.IsNullOrWhiteSpace(typName)) 
                return new KategorieVozidlaTyp("neuvedeno", "fa-circle-question");
            if (_typy.TryGetValue(typName.Trim().ToLower(), out var typ))
                return typ;
            return new KategorieVozidlaTyp("neuvedeno", "fa-circle-question");
        }
        private static readonly Dictionary<string, KategorieVozidlaTyp> _typy =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["motocykl"] = new("Motocykl", "fa-motorcycle"),
                ["auto"] = new("Auto", "fa-car-side"),
                ["autobus"] = new("Autobus", "fa-bus-side"),
                ["nákladní vůz"] = new("Nákladní vůz", "fa-van"),
                ["přípojné vozidlo"] = new("Přípojné vozidlo", "fa-trailer"),
                ["traktor"] = new("Traktor", "fa-tractor"),
                ["ostatni"] = new("Ostatní", "fa-excavator"),
                ["neuvedeno"] = new("Neuvedeno", "fa-circle-question"),
            };  

        private static readonly Dictionary<string, KategorieVozidlaInfo> _map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["L1"] = new("L1", "Vozidla se dvěma koly, šlapátky a objemem válců nepřesahujícím 50 cm3 - mopedy.", GetTyp("motocykl")),
                ["L2"] = new("L2", "Vozidla se třemi koly a objemem válců nepřesahujícím 50 cm3.", GetTyp("motocykl")),
                ["L3"] = new("L3", "Vozidla se dvěma koly a pevnými stupačkami- skútry a motocykly.", GetTyp("motocykl")),
                ["L4"] = new("L4", "Vozidla se třemi koly umístěnými nesouměrně k podélné střední rovině vozidla a objemem válců nepřesahujícím 50 cm3.", GetTyp("motocykl")),
                ["L5"] = new("L5", "Vozidla se třemi koly umístěnými souměrně k podélné střední rovině vozidla a objemem válců přesahujícím 50 cm3.", GetTyp("motocykl")),
                
                ["M1"] = new("M1", "Vozidla, která mají nejvýše 8 míst k přepravě osob, kromě místa řidiče nebo víceúčelová vozidla", GetTyp("auto")),
                ["M2"] = new("M2", "Vozidla, která mají více než 8 míst k přepravě osob, kromě místa řidiče a jejichž největší přípustná hmotnost nepřesahuje 5000 kg", GetTyp("autobus")),
                ["M3"] = new("M3", "Vozidla, která mají více než 8 míst k přepravě osob, kromě místa řidiče a jejichž největší přípustná hmotnost přesahuje 5000 kg", GetTyp("autobus")),

                ["N1"] = new("N1", "Vozidla, jejichž největší přípustná hmotnost nepřevyšuje 3 500 kg.", GetTyp("Nákladní vůz")),
                ["N2"] = new("N2", "Vozidla, jejichž největší přípustná hmotnost převyšuje 3 500 kg, avšak nepřevyšuje 12 000 kg.", GetTyp("Nákladní vůz")),
                ["N3"] = new("N3", "Vozidla, jejichž největší přípustná hmotnost převyšuje 12 000 kg.", GetTyp("Nákladní vůz")),

                ["O1"] = new("O1", "Přípojná vozidla, jejichž největší přípustná hmotnost nepřevyšuje 750 kg.", GetTyp("Přípojné vozidlo")),
                ["O2"] = new("O2", "Přípojná vozidla, jejichž největší přípustná hmotnost převyšuje 750 kg, ale nepřevyšuje 3 500 kg.", GetTyp("Přípojné vozidlo")),
                ["O3"] = new("O3", "Přípojná vozidla, jejichž největší přípustná hmotnost převyšuje 3 500 kg, ale nepřevyšuje 10 000 kg.", GetTyp("Přípojné vozidlo")),
                ["O4"] = new("O4", "Přípojná vozidla, jejichž největší přípustná hmotnost převyšuje 10 000 kg.", GetTyp("Přípojné vozidlo")),

                [""] = new("", "Položka nebyla naplněna.", GetTyp("Neuvedeno")),

                ["PT"] = new("PT", "Přívěs traktorový - kategorie 1,2,3,4", GetTyp("Přípojné vozidlo")),
                ["NT"] = new("NT", "Návěs traktorový - kategorie 1,2,3,4", GetTyp("Přípojné vozidlo")),
                ["OT"] = new("OT", "Přípojná vozidla traktoru.", GetTyp("Přípojné vozidlo")),

                ["R"] = new("R", "Ostatní silniční vozidla.", GetTyp("Ostatni")),

                ["LA"] = new("LA", "MOPED, SKÚTR, MOKIK, MOTOCYKL SPORTOVNÍ a MOTOKOLO; jejich nejvyšší konstrukční rychlost není větší než 45 km.h-1; je-li poháněn spalovacím motorem, nesmí být jeho zdvihový nebo jemu rovnocenný objem větší než 50 cm3.",GetTyp("motocykl")),
                ["LB"] = new("LB", "MOPED - TŘÍKOLKA NEBO LEHKÁ ČTYŘKOLKA; tříkolové nebo čtyřkolové vozidlo splňující podmínky ustanovení přílohy Zákona 56/2001 Sb.",GetTyp("motocykl")),
                ["LC"] = new("LC", "MOTOCYKL, SKÚTR a MOTOCYKL SPORTOVNÍ pro dopravu jedné nebo dvou osob sedících za sebou; se dvěma koly a pevnými stupačkami.",GetTyp("motocykl")),
                ["LD"] = new("LD", "MOTOCYKL S POSTRANNÍM VOZÍKEM; vozidlo se třemi koly uspořádanými nesouměrně vzhledem k střední podélné rovině; maximální konstrukční rychlost přesahuje 45 km.h-1; objem válců přesahuje 50 cm3.",GetTyp("motocykl")),
                ["LE"] = new("LE", "TŘÍKOLKA, ČTYŘKOLKA; tříkolové nebo čtyřkolové vozidlo splňující podmínky ustanovení přílohy Zákona 56/2001 Sb.",GetTyp("motocykl")),
                ["LM"] = new("LM", "MOTOKOLO; jízdní kolo opatřené trvale připojeným hnacím motorem, jehož nejvyšší konstrukční rychlost nepřekročí 25 km.h-1.",GetTyp("motocykl")),

                ["T1"] = new("T1", "Traktory s maximální konstrukční rychlostí nepřevyšující 40 km.h-1, s nejméně jednou nápravou a s minimálním rozchodem větším než 1150 mm, s nenaloženou hmotností v provozním stavu větší než 600 kg a se světlou výškou nad vozovkou menší než 1000 mm.", GetTyp("Traktor")),
                ["T2"] = new("T2", "Traktory - viz T1 - se světlou výškou nad vozovkou menší než 600 mm. Pokud je výška těžiště traktoru (měřeno vůči vozovce) dělená střední hodnotou minimálního rozchodu všech náprav větší než 0.9, je maximální konstrukční rychlost omezena na 30 km.h-1.", GetTyp("Traktor")),
                ["T3"] = new("T3", "Traktory s maximální konstrukční rychlostí nepřevyšující 40 km.h-1 a s nenaloženou hmotností menší než 600 kg.", GetTyp("Traktor")),
                ["T4"] = new("T4", "Ostatní traktory s maximální konstrukční rychlostí nepřevyšující 40 km.h-1.", GetTyp("Traktor")),

                ["SS"] = new("SS", "Pracovní stroj s vlastním zdrojem pohonu konstrukčně svým vybavením určený pouze pro vykonávání určitých pracovních činností. Není zpravidla určený pro přepravní činnost.", GetTyp("Ostatni")),
                ["SP"] = new("SP", "Pracovní stroj přípojný bez vlastního zdroje pohonu konstrukčně svým vybavením určený pouze pro vykonávání určitých pracovních činností. Připojuje se k tažnému mot. vozidlu, přizpůsobenému k jeho připojení. Není zpravidla určený pro přepravní činnost.", GetTyp("Ostatni")),
            };

        /// <summary>
        /// Vrátí (zkratka, popis, typ) pro danou zkratku. Neznámé zkratky -> null.
        /// </summary>

        private static char[] firstCodeChars = null;
        public static KategorieVozidlaInfo? Get(string? code)
        {
            if (firstCodeChars == null)
            {
                firstCodeChars = _map.Keys.Select(k => k.FirstOrDefault()).Distinct().ToArray();
            }

            if (string.IsNullOrWhiteSpace(code)) return null;
            code = code.Trim();

            //special case: empty string is a valid code
            if (firstCodeChars.Contains(code.FirstOrDefault()) == false)
                return _map.TryGetValue("", out var emptyInfo) ? emptyInfo : null;

            //special case: empty string is a valid code
            if (code.FirstOrDefault() == 'R')
                return _map.TryGetValue("R", out var rInfo) ? rInfo : null;


            return _map.TryGetValue(code.ShortenMe(2, "", "").ToUpper(), out var info) ? info : null;
        }

        /// <summary>
        /// Varianta bez nullable: neznámé zkratky -> (code, "", "").
        /// </summary>
        public static KategorieVozidlaInfo GetOrDefault(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) 
                return default;
            code = code.Trim();
            return Get(code) ?? default;
        }
    }
}