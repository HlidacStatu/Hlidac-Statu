using System.Dynamic;
using System.Text.Json;
using Devmasters;
using HlidacStatu.AI.LLM;
using HlidacStatu.AI.LLM.Clients.Options;
using HlidacStatu.Entities;
using HlidacStatu.Entities.AI;
using HlidacStatu.Repositories;
using Serilog;
using Serilog.Core;

namespace HlidacStatu.Extensions;

public static class DotaceExtension
{
    private static readonly ILogger _logger = Log.ForContext(typeof(DotaceExtension));
    
    public static async Task<bool?> MaSkutecnehoMajiteleAsync(this Dotace dotace)
    {
        if (dotace.ApprovedYear is null)
            return null;
        if (string.IsNullOrWhiteSpace(dotace.Recipient.Ico))
            return null;

        var datum = new DateTime(dotace.ApprovedYear.Value, 1, 1);
        var firma = FirmaRepo.FromIco(dotace.Recipient.Ico);

        if (SkutecniMajiteleRepo.PodlehaSkm(firma, datum))
        {
            var result = await SkutecniMajiteleRepo.GetAsync(firma.ICO);

            //skm nenalezen
            if (result == null)
                return false;
        }

        return true;
    }

    public static ExpandoObject FlatExport(this Dotace subsidy)
    {
        dynamic v = new ExpandoObject();
        v.Id = subsidy.Id;
        v.DataSource = subsidy.PrimaryDataSource;
        v.Url = subsidy.GetUrl(false);
        v.AssumedAmount = subsidy.AssumedAmount;
        v.RecipientIco = subsidy.Recipient.Ico;
        v.RecipientName = subsidy.Recipient.Name;
        v.RecipientHlidacName = subsidy.Recipient.HlidacName;
        v.RecipientYearOfBirth = subsidy.Recipient.YearOfBirth;
        v.RecipientObec = subsidy.Recipient.Obec;
        v.RecipientOkres = subsidy.Recipient.Okres;
        v.RecipientPSC = subsidy.Recipient.PSC;
        v.SubsidyAmount = subsidy.SubsidyAmount;
        v.PayedAmount = subsidy.PayedAmount;
        v.ReturnedAmount = subsidy.ReturnedAmount;
        v.ProjectCode = subsidy.ProjectCode;
        v.ProjectName = subsidy.ProjectName;
        v.ProjectDescription = subsidy.ProjectDescription;
        v.ProgramCode = subsidy.ProgramCode;
        v.ProgramName = subsidy.ProgramName;
        v.ApprovedYear = subsidy.ApprovedYear;
        v.SubsidyProvider = subsidy.SubsidyProvider;
        v.SubsidyProviderIco = subsidy.SubsidyProviderIco;
        v.HintIsOriginal = subsidy.Hints.IsOriginal;
        v.HintsOriginalSubsidyId = subsidy.Hints.OriginalSubsidyId;
        v.HintsHasDuplicates = subsidy.Hints.HasDuplicates;
        v.HintsCategory1 = subsidy.Hints.Category1;
        v.HintsCategory2 = subsidy.Hints.Category2;
        v.HintsCategory3 = subsidy.Hints.Category3;
        v.HintsRecipientStatus = subsidy.Hints.RecipientStatus;
        v.HintsSubsidyType = subsidy.Hints.SubsidyType;
        v.HintsRecipientStatusFull = subsidy.Hints.RecipientStatusFull;
        v.HintsRecipientTypSubjektu = subsidy.Hints.RecipientTypSubjektu;
        v.HintsRecipientPolitickyAngazovanySubjekt = subsidy.Hints.RecipientPolitickyAngazovanySubjekt;
        v.HintsRecipientPocetLetOdZalozeni = subsidy.Hints.RecipientPocetLetOdZalozeni;


        return v;
    }

    public static string? GetNiceRawData(this Subsidy subsidy)
    {
        if (subsidy.RawData is null)
            return null;

        return JsonSerializer.Serialize(subsidy.RawData, new JsonSerializerOptions
        {
            WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public static string? DescribeDataSource(string primaryDataSource)
    {
        if (Dotace.DataSourceDescription.TryGetValue(primaryDataSource, out var description))
        {
            return description;
        }

        return string.Empty;
    }

    public static string? DescribeDataSource(this Dotace subsidy)
    {
        return DescribeDataSource(subsidy.PrimaryDataSource);
    }

    public static async Task<IEnumerable<Dotace.Hint.Category>> ToCalculatedCategoryWithAIAsync(this Dotace item)
    {
        if (item == null)
            return Array.Empty<Dotace.Hint.Category>();
        if (string.IsNullOrWhiteSpace(item.ProjectName + " " + item.ProjectDescription))
            return Array.Empty<Dotace.Hint.Category>();

        List<Dotace.Hint.Category> cats = new();

        Uri ollamaUri = new Uri("http://10.10.150.163:7862");
        HlidacStatu.AI.LLM.Clients.OllamaServerClient<CoreOptions> llm =
            new HlidacStatu.AI.LLM.Clients.OllamaServerClient<CoreOptions>(ollamaUri.AbsoluteUri);
        HlidacStatu.AI.LLM.Models.Model model = HlidacStatu.AI.LLM.Models.Model.Llama31;
        var llmQuery = new LLMQuery(
            llmCategoryQuery.Replace("##TEXT##", item.ProjectName + " " + item.ProjectDescription),
            "Odpovídej stručně, přesně, co nejpřesněji podle instrukcí.Pokud nevíš nebo si nejsi jist, odpověz pouze #NEVIM#",
            temperature: 0.3f, responseFormat: null);

        string resFull = "";
        int runs = 0;
        start:
        runs++;
        try
        {
            resFull = await llm.QueryAsync(llmQuery, null);
        }
        catch (Exception e)
        {
            _logger.Error(e, "llmquery {query}", llmQuery.Query);
        }


        resFull = resFull.Trim().ToLower().RemoveAccents().Replace(" ", "");
        if (resFull.EndsWith("."))
            resFull = resFull.Remove(resFull.Length - 1);
        resFull = resFull.Replace("<cat>", "").Replace("</cat>", "");
        resFull = resFull.Replace("<", "").Replace(">", "");

        //{"kategorie": "Životní prostředí, klima a ekologické projekty"}

        _logger.Information("{id}: AI result {category} for project {projectName}", item.Id, resFull,
            (item.ProjectName + " " + item.ProjectDescription).ShortenMe(30));

        foreach (var kv in catsDescriptions)
        {
            if (
                resFull.Contains(kv.Key, StringComparison.InvariantCultureIgnoreCase)
                || resFull.Contains("<cat>" + kv.Key + "</cat>", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                cats.Add(new Dotace.Hint.Category()
                    { Created = DateTime.Now, Probability = 0.8m, TypeValue = (int)kv.Value });
                break;
            }
        }

        if (cats.Count == 0 && runs < 6)
            goto start;


        return cats;
    }


    static string llmCategoryQuery = @"Máš k dispozici pouze tyto kategorie, mezi znackami
    <categories></categories>. Každá kategorie je uvedena na samostatném řádku: 

    <categories> 

    <cat>Digi</cat>  Digitální transformace, IT, výpočetní technika, software,
    cloud, AI, umělá inteligence, informační systém, IT infrastruktura, datové
    centrum

    <cat>Doprava</cat> Doprava a infrastruktura, železnice, stanice,
    rekonstrukce, nákup autobusů, nákup vlaků, nákup vozidla, budova, násep,
    zařízení pro údržbu silnic, Dálnice D1, silnice, okruh

    <cat>Energetika</cat>  Energetika a obnovitelné zdroje, obnovitelné zdroje
    energie, dodávku elektřiny a plynu, uhlí, ropy, přenosová soustava,
    energetická soustava, elektrárna

    <cat>FondHejtmana</cat>  Fond hejtmana

    <cat>FondPrimatora = 52000 Fond primátora

    <cat>HumanitarniPomoc</cat>  Humanitární pomoc a rozvojová spolupráce,
    Charita, uprchlíci, válečný konflikt, afrika, Ukrajina, Pomoc
    Ukrajině,červený kříž

    <cat>InovaceVyzkum</cat> Inovace, výzkum a vývoj, Podpora VO, 

    <cat>Kriminalita</cat> Prevence kriminality a bezpečnost

    <cat>Kultura</cat> Kultura, kreativní průmysly a média, galerie, divadlo,
    představení, zpěv, recitace, festival

    <cat>Obrana</cat>  Obrana a bezpečnost

    <cat>Pamatky</cat> Památková péče a cestovní ruch, turismus, cestovní
    kancelář, obnova památek, obnova sochy, rekonstrukce budovy, rekonstrukce
    zámku, turistický areál

    <cat>PodporaPodnikani</cat>  Podpora podnikání a investic

    <cat>PravniStat</cat>  Podpora demokracie a právního státu, volby,
    transparentnost, LGBT, vyloučené skupiny, znevýhodněné skupiny obyvatel

    <cat>RegionalniRozvoj</cat>  Regionální rozvoj, podpora, investice a
    soudržnost, Vybudování chodníku, Rekonstrukce MVN, obnova, vodovod, školní
    kuchyně, Rekonstrukce cest, hasiči, kapacita stanice, koupaliště

    <cat>SocialniSluzby</cat>  Sociální služby, začleňování a zaměstnanost,
    dlouhodobě nemocní, hospic, domov pro postižené, domov pro seniory, dům, 

    <cat>Sport</cat> Sport a volnočasové aktivity, sportovní areál, cvičiště,
    sportovní závody, soutěž, stadión, běh

    <cat>TrhPrace</cat>  Podpora zaměstnanosti a trhu práce, odborné
    vzdělávání, 

    <cat>VerejnaSprava</cat> Investiční a dotační programy pro veřejnou správu

    <cat>Vzdelavani</cat>  Vzdělávání,školství a mládež, školy, družiny, učitelé

    <cat>Zahranici</cat> Bilaterální zahraniční rozvojové spolupráce (ZRS),
    rozvoj občanské společnosti, přeshraniční spolupráce, 

    <cat>Zdravotnictvi</cat> Zdraví a zdravotnické projekty, geriatrického
    oddělení, léčebna, Rehabilitace, nemocnice, léky, zdravotní zařízení,
    oddělení

    <cat>Zemedelstvi</cat> Zemědělství, lesnictví a rozvoj venkova, SZIF, les,
    remízek, stromy, lesní hospodářství, myslivost

    <cat>ZivotniProstredi</cat>  Životní prostředí, klima a ekologické projekty,
    podporu obnovitelných zdrojů, OZE, Odstraňování následků těžby, ZNHČ,
    povodeň, kvality vod, ochrana vod, Zateplení 

    </categories> 


    Analyzuj text uvedený mezi značkami <text></text> a vyber nejvhodnější
    kategorii z uvedeného seznamu. Nevysvětluj svůj postoj, neopakuj text, pouze
    uveď název nejvhodnější kategorie uvedený mezi znaky <cat></cat>.

    <text>##TEXT##</text>";
    
    static Dictionary<string, Dotace.Hint.CalculatedCategories> catsDescriptions = Enum.GetValues<Dotace.Hint.CalculatedCategories>()
        .Select(m => new { key = m.ToString(), val = m })
        .ToDictionary(k => k.key, v => v.val);
}