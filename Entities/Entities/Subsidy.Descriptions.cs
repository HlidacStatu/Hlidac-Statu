using System.Collections.Generic;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    
    public static readonly Dictionary<string, string> DataSourceDescription = new()
    {
        // statni
        ["Cedr"] = "Jedná se o starý (neaktivní) informační zdroj, který může obsahovat chyby. Tento zdroj jsme se rozhodli zachovat čistě z archivačního důvodu. Jeho náhradou s aktuálními a opravenými daty je zdroj IsRed.",
        ["CzechInvest"] = "Tento zdroj obsahuje pouze data s investičními pobídkami. Nejedná se přímo o dotace. Z našeho uvážení jsme se však rozhodli zařadit mezi dotace. Částka rozhodnutá zde nemusela být vůbec vyčerpána v plné výši.",
        ["DeMinimis"] = "",
        ["DotInfo"] = "Jedná se o starý (neaktivní) informační zdroj, který může obsahovat hodně chyb. Tento zdroj jsme se rozhodli zachovat čistě z archivačního důvodu.",
        ["Eufondy"] = "Aktivní zdroj. Neobsahuje data v nejlepší kvalitě a proto, pokud existuje duplicita v tomto detailu, která vede na IsRed, doporučujeme se podívat na přesnější informace právě ze systému IsRed.",
        ["IsRed"] = "Aktivní zdroj, který funguje jako centrální registr dotací. Sem by měly být nahrávány všechny dotace poskytované státem, nebo EU. Datově nejkvalitnější zdroj.",
        ["Statni zemedelsky intervencni fond"] = "Aktivní zdroj. Kvalita dat je zde mizerná a data starší tří let už neopraví. Proto, pokud existuje duplicita v tomto detailu, která vede na IsRed, doporučujeme se podívat na přesnější informace právě ze systému IsRed.",
        
        // krajske
        ["Hlavni mesto Praha"] = "Kvalitní zdroj s open source daty. Složitější (trochu nejasný) způsob exportu. V csv exportu se v jednom souboru vyskytuje více tabulek, takže tento export je nutné ručně rozdělit.",
        ["Jihocesky Kraj"] = "Kvalitní zdroj, bohužel bez open dat. Musíme pochválit snahu a vstřícnou komunikaci, kde nám strukturu sestavili na přání.",
        ["Jihomoravsky Kraj"] = "Dodali data v celkem dobré kvalitě. U některých položek jsme museli chvíli luštit, jak je správně interpretovat.",
        ["Karlovarsky Kraj"] = "Dodali data v námi požadované struktuře a dobré kvalitě. Musíme pochválit i rychlost dodání informací. Domníváme se, že tento kraj neposlal informace o všech dotacích.",
        ["Kraj Vysocina"] = "Dodali data ve struktuře velmi podobné tomu, co jsme požadovali. Kvalita dat nám přijde také dobrá.",
        ["Kralovehradecky Kraj"] = "Odpověď jim trvala trochu déle, ale dodali data v námi požadované struktuře a dobré kvalitě.",
        ["Liberecky Kraj"] = "Na to, že jsme za data platilili, tak jejich kvalita (co odbor, to jiný soubor) je celkem mizerná. Často se mění hlavičky, v souborech se vyskytují sumy, některá data jsou v řádcích namísto ve sloupcích. Na druhou stranu musíme pochválit proaktivní snahu přijít s jednotným exportem do budoucna.",
        ["Moravskoslezsky Kraj"] = "Data dodali po odborech v různých formách a rozdílné kvalitě. Nalezli jsme několik chyb, které jsme ve spolupráci s krajem celkem rychle opravili.",
        ["Pardubicky Kraj"] = "Kvalita dat z tohoto zdroje je celkem mizerná. Museli jsme parsovat účetní záznamy a anonymizovat! rodná čísla.",
        ["Plzensky Kraj"] = "Kvalitní zdroj s open source daty. Open source data byli schopni vytvořit v rekordním čase.",
        ["Stredocesky Kraj"] = "Kvalitní zdroj s open source daty. Jediné co zde mohu vytknout je odkaz na sharepoint namísto přímého stažení dat.",
        ["Ustecky Kraj"] = "Data dodali v celkem dobré struktuře. Interpretaci některých údajů jsme si museli ověřit. Ve zdroji jsme nalezli několik (potencionálních) chyb, ke kterým stále nemáme vyjádření.",
        ["Zlinsky Kraj"] = "Po drobném upřesnění poslali data v dobré kvalitě v požadovaném formátu. Tento kraj podezříváme, že nám neposlal informace o všech dotacích. Chybí nám například informace o kotlíkových dotacích.",

    };
    
    public static readonly Dictionary<string, string> DataSourceLinks = new()
    {
        // statni
        ["Cedr"] = "Odkaz už neexistuje",
        ["CzechInvest"] = "https://www.czechinvest.gov.cz",
        ["DeMinimis"] = "",
        ["DotInfo"] = "Odkaz už neexistuje",
        ["Eufondy"] = "https://www.dotaceeu.cz/cs/evropske-fondy-v-cr, https://ms14opendata.mssf.cz, https://ms21opendata.mssf.cz",
        ["IsRed"] = "https://red.fs.gov.cz/",
        ["Statni zemedelsky intervencni fond"] = "https://szif.gov.cz/cs/seznam-prijemcu-dotaci",
        
        // krajske
        ["Hlavni mesto Praha"] = "https://granty.praha.eu/GrantyPortalRS/Explorer/default.aspx",
        ["Jihocesky Kraj"] = "",
        ["Jihomoravsky Kraj"] = "https://krajbezkorupce.kr-jihomoravsky.cz/Folders/1152-1-Dotace.aspx",
        ["Karlovarsky Kraj"] = "",
        ["Kraj Vysocina"] = "",
        ["Kralovehradecky Kraj"] = "",
        ["Liberecky Kraj"] = "",
        ["Moravskoslezsky Kraj"] = "",
        ["Pardubicky Kraj"] = "",
        ["Plzensky Kraj"] = "https://opendata.plzensky-kraj.cz/dotace/",
        ["Stredocesky Kraj"] = "https://stredoceskykraj.cz/web/urad/prehled-poskytnutych-dotaci",
        ["Ustecky Kraj"] = "",
        ["Zlinsky Kraj"] = "",

    };
}