using System;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Devmasters.Enums;

namespace PlatyUredniku.Controllers;

public class UredniciController : Controller
{
    public async Task<IActionResult> Analyza(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Redirect("/analyzy");
        
        switch (id.ToLower())
        {
            case "kategorie":
                ViewBag.Title = "Porovnání a vývoj platů v různých kategoriích zaměstnání";
                break;
            case "namestciministerstev":
                ViewBag.Title = "Porovnání odměn náměstků na ministerstvech";
                break;
            case "nejvyssiodmeny":
                ViewBag.Title = "Platy s nejvyšším podílem odměny na celkovém platu";
                break;
            case "zmenanejvyssihoplatu":
                if (Request.Query["max"] == "false")
                    ViewBag.Title = "Přehled nejvyšších poklesů manažerských platů";
                else
                    ViewBag.Title = "Přehled nejvyšších nárůstů manažerských platů";
                break;
            case "zmenaplatuceos":
                if (Request.Query["max"]=="false")
                    ViewBag.Title = "Přehled nejvyšších poklesů ředitelských platů";
                else
                    ViewBag.Title = "Přehled nejvyšších nárůstů ředitelských platů";
                break;
            default:
                break;
        }
        return View("Analyza_"+id);

    }
    public async Task<IActionResult> Analyzy()
    {
        ViewBag.Title = "Podrobné analýzy";

        return View();
    }
    public async Task<IActionResult> Index()
    {
        return View();
    }
    

    public async Task<IActionResult> DlePlatu(int id)
    {
        int prumernyPlat = 46_013;
        (int Min, int Max) range = (0, int.MaxValue);
        string title = "Manažerské platy ve veřejné správě";
        string noteHtml = "";
        string rozsah = "";
        string odkaz = "";
        switch (id)
        {
            case 2:
                range = (0_000, prumernyPlat);
                title = $"Manažerské platy ve veřejné správě nižší než průměrný plat v ČR za rok {PuRepo.DefaultYear}";
                noteHtml = "Průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a>v Q4 2023 byl <b>46 013 Kč hrubého</b>.";
                rozsah = $"Rozsah zobrazovaných platů manažerů ve veřejné správě je od nuly až po průměrný plat (<b>46 013 Kč</b>).";
                odkaz = $"<a href=\"/DlePlatu/3\">Vyšší než průměrné platy</a>";
                break;
            case 3:
                range = (prumernyPlat, prumernyPlat * 2);
                title = $"Manažerské platy ve veřejné správě vyšší než průměrný plat v ČR za rok {PuRepo.DefaultYear}";
                noteHtml = "Průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a>v Q4 2023 byl <b>46 013 Kč hrubého</b>.";
                rozsah = $"Rozsah zobrazovaných platů manažerů ve veřejné správě je od průměrného platu (<b>46 013 Kč</b>) po dvojnásobek průměrného platu (<b>92 026 Kč</b>).";
                odkaz = $"<a href=\"/DlePlatu/4\">Nejvyšší platy ve veřejné správě</a>";
                break;
            case 4:
                range = (prumernyPlat * 2, 100_000_000);
                title = $"Nejvyšší manažerské platy ve veřejné správě za rok {PuRepo.DefaultYear}";
                noteHtml = "Zobrazujeme platy manažerů, které jsou více než dvojnásobné, než je průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a>(46 013 Kč hrubého).";
                rozsah = $"Zobrazované platy manažerů ve veřejné správě jsou větší než dvojnásobek průměrného platu (<b>92 026 Kč</b>).";
                break;
            case 1:
            default:
                range = (0_000, 35_300);
                title = $"Manažerské platy ve veřejné správě za rok {PuRepo.DefaultYear} nižší než nástupní plat pokladní/ho v Lidlu ";
                noteHtml = "Nástupní plat pokladní v Lidl v Praze byl <a href='https://spolecnost.lidl.cz/pro-novinare/tiskove-zpravy/spolecnost-lidl-navysuje-mzdy-a-rozsiruje-benefity' target='_blank'>podle iDnes</a> <b>35 300 Kč hrubého</b>.";
                rozsah = $"Rozsah zobrazovaných platů manažerů ve veřejné správě je od nuly až po nástupní plat pokladní/ho v Lidlu (<b>35 300 Kč</b>).";
                odkaz = $"<a href=\"/DlePlatu/2\">Nižší než průměrné platy</a>";
                break;
        }

        var platy = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetPoziceDlePlatuCached(range.Min, range.Max, PuRepo.DefaultYear);
        var platyCount = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetPlatyCountPerYearCached(PuRepo.DefaultYear);

        ViewData["rozsah"] = rozsah;
        ViewData["odkaz"] = odkaz;
        ViewData["platyMaximum"] = range.Max;
        ViewData["title"] = title;
        ViewData["noteHtml"] = noteHtml;
        ViewData["pocetPlatuCelkem"] = platyCount;

        ViewBag.Title = title;

        return View(platy);
    }

    public async Task<IActionResult> Oblast(string id)
    {
        var normalizedTag = PuOrganizaceTag.NormalizeTag(id);
        if (string.IsNullOrWhiteSpace(normalizedTag))
            return NotFound();
        
        var organizace = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetOrganizaceForTagCached(normalizedTag);

        var tag = await PuRepo.GetTagAsync(normalizedTag);
        var oblast = tag is null ? id : tag.Tag;
        
        ViewData["oblast"] = oblast;

        if (tag is not null && 
            tag.TagNormalized.Equals(tag.Tag, StringComparison.InvariantCultureIgnoreCase) == false)
        {
            ViewData["CanonicalUrl"] = $"https://platy.hlidacstatu.cz{Url.Action("Oblast", new { id = tag.TagNormalized })}";
        }

        ViewBag.Title = "Platy a organizace v oblasti #" + oblast;

        return View(organizace);
    }

    public async Task<IActionResult> Oblasti()
    {
        var sw = new Stopwatch();
        sw.Start();
        var oblasti = PuRepo.MainTags;
        var model = new Dictionary<string, List<PuOrganizace>>();
        foreach (var oblast in oblasti)
        {
            var organizace = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetOrganizaceForTagCached(oblast);

            model.Add(oblast, organizace);
        }

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = nameof(Oblasti),
                Link = $"{nameof(Oblasti)}"
            }
        };

        ViewData["breadcrumbs"] = breadcrumbs;
        sw.Stop();
        ViewData["sw"] = sw.ElapsedMilliseconds;

        ViewBag.Title = "Platy a organizace v různých oborech";

        return View(model);
    }

    public async Task<IActionResult> Detail(string id, int? rok = null)
    {
        var detail = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetFullDetailOrganizaceCached(id);
        
        ViewBag.Title = detail.Nazev;

        ViewData["mainTag"] = detail.Tags.FirstOrDefault(t => PuRepo.MainTags.Contains(t.Tag))?.Tag;
        ViewData["id"] = id;

        return View(detail);
    }

    public async Task<IActionResult> Plat(int id)
    {
        var detail = await HlidacStatu.Extensions.Cache.Platy.Urednici.GetPlatCached(id);

        ViewData["mainTag"] = detail.Organizace.Tags.FirstOrDefault(t => PuRepo.MainTags.Contains(t.Tag))?.Tag;

        ViewBag.Title = $"Plat {detail.NazevPozice} v {detail.Organizace.Nazev}";

        return View(detail);
    }

    
    public IActionResult Statistika(string id, string typ)
    {
        return View("statistika_"+id,typ);
    }
    
    
    public IActionResult OpenData()
    {
        ViewBag.Title = $"Open data";

        return View();
    }

    public async Task<IActionResult> Export(string type, string? datovaSchranka, int? rok)
    {
        byte[] rawData = null;
        string contentType = "";
        string filename = "";

        if (!(type == "excel" || type == "tsv"))
            return NoContent();

        
        var platyRaw = await PuRepo.ExportAllAsync(datovaSchranka, rok);
        var minYear = rok.HasValue ? rok.Value : PuRepo.MinYear;
        var maxYear = rok.HasValue ? rok.Value : PuRepo.DefaultYear;

        List<dynamic> data = new List<dynamic>();
        foreach (var organizace in platyRaw)
        {
            var tags = string.Join(", ", organizace.Tags.Select(t => t.Tag));
            for (var forYear = minYear; forYear <= maxYear; forYear++)
            {
                var metadataForYear = organizace.Metadata.FirstOrDefault(r => r.Rok == forYear);
                var platyForYear = organizace.Platy.Where(r => r.Rok == forYear).ToList();
                
                if(platyForYear.Any())
                {
                    foreach (var plat in platyForYear)
                    {
                        data.Add(new {
                            organizace.Ico,
                            NazevOrganizace = organizace.Nazev,
                            DatovaSchranka = organizace.DS,
                            Rok = forYear,
                            NazevPozice = plat.NazevPozice?.ReplaceLineEndings(" "),
                            plat.Plat,
                            plat.Odmeny,
                            plat.PocetMesicu,
                            plat.Uvazek,
                            NefinancniBonus = plat.NefinancniBonus?.ReplaceLineEndings(" "),
                            PoznamkaPlat = plat.PoznamkaPlat?.ReplaceLineEndings(" "),
                            JeReditel = plat.JeHlavoun == true ? "Ano" : "Ne",
                            Tagy = tags,
                            DatumOdeslaniZadosti = metadataForYear?.DatumOdeslaniZadosti,
                            DatumPrijetiOdpovedi = metadataForYear?.DatumPrijetiOdpovedi,
                            PoznamkaHlidace = metadataForYear?.PoznamkaHlidace?.ReplaceLineEndings(" "),
                            ZduvodneniMimoradnychOdmen = metadataForYear?.ZduvodneniMimoradnychOdmen == true ? "Ano" : "Ne",
                            Komunikace = metadataForYear?.ZpusobKomunikace?.ToNiceDisplayName()
                        });
                    }
                }
                else if (metadataForYear is not null)
                {
                    data.Add(new {
                        organizace.Ico,
                        NazevOrganizace = organizace.Nazev,
                        DatovaSchranka = organizace.DS,
                        Rok = forYear,
                        NazevPozice = "",
                        Plat = 0,
                        Odmeny = 0,
                        PocetMesicu = 0,
                        Uvazek = 0,
                        NefinancniBonus = "",
                        PoznamkaPlat = "",
                        JeReditel = "",
                        Tagy = tags,
                        DatumOdeslaniZadosti = metadataForYear?.DatumOdeslaniZadosti,
                        DatumPrijetiOdpovedi = metadataForYear?.DatumPrijetiOdpovedi,
                        PoznamkaHlidace = metadataForYear?.PoznamkaHlidace?.ReplaceLineEndings(" "),
                        ZduvodneniMimoradnychOdmen = metadataForYear?.ZduvodneniMimoradnychOdmen == true ? "Ano" : "Ne",
                        Komunikace = metadataForYear?.ZpusobKomunikace?.ToNiceDisplayName()
                    });
                }
            }
        }

        switch (type)
        {
            case "excel":
                rawData = new HlidacStatu.ExportData.Excel().ExportData(new HlidacStatu.ExportData.Data(data));
                contentType = "application/vnd.ms-excel";
                filename = "export.xlsx";
                break;
            case "tsv":
                rawData = new HlidacStatu.ExportData.TabDelimited().ExportData(new HlidacStatu.ExportData.Data(data));
                contentType = "text/tab-separated-values";
                filename = "export.tsv";
                break;
            default:
                return NoContent();
        }

        return File(rawData, contentType, filename);
    }


}