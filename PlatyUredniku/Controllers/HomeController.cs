using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;
using System.Text;
using System;
using Microsoft.AspNetCore.OutputCaching;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    private readonly IFusionCache _cache;

    public HomeController(IFusionCache cache)
    {
        _cache = cache;
    }
    
    public async Task<IActionResult> Index()
    {
        var platyTask = _cache.GetOrSetAsync<List<PuPlat>>(
            $"{nameof(PuRepo.GetPlatyAsync)}_{PuRepo.DefaultYear}",
            _ => PuRepo.GetPlatyAsync(PuRepo.DefaultYear)
        );

        ViewData["platy"] = await platyTask;
        if (this.User.IsInRole("Admin"))
            return View("IndexNew");
        else
            return View();
    }


    public IActionResult OpenData()
    {
        ViewBag.Title = $"Open data";

        return View();
    }
    

    [OutputCache(Duration = 3600*24*7)]
    public async Task<IActionResult> SiteMap()
    {
        string modif = DateTime.Now.Date.ToString("yyyy-MM-dd") + "T09:00:00+00:00";
        StringBuilder sb = new StringBuilder(1024*20);
        sb.AppendLine(@$"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.sitemaps.org/schemas/sitemap/0.9
            http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"">
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/</loc>
        <lastmod>{modif}</lastmod>
        <priority>1.00</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/Texty/OProjektu</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/Texty/PlatyStatnichZamestnancu</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/Texty/Nejvyssi</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>

    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/DlePlatu/1</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/DlePlatu/2</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/DlePlatu/3</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/DlePlatu/4</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/analyzy</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platyuredniku.hlidacstatu.cz/analyza/</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>

");
        foreach (var item in new string[]{ 
            "ZmenaPlatuCeos","ZmenaPlatuCeos?max=false","ZmenaNejvyssihoPlatu","ZmenaNejvyssihoPlatu?max=false",
                "NejvyssiOdmeny","kategorie","kategorie?k1=23" }
        )
        {
            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>https://platyuredniku.hlidacstatu.cz/analyza/{System.Security.SecurityElement.Escape(item)}</loc>");
            sb.AppendLine($"<lastmod>{modif}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
        }

        foreach (var item in PuRepo.MainTags)
        {
            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>https://platyuredniku.hlidacstatu.cz/Oblast/{System.Security.SecurityElement.Escape(item)}</loc>");
            sb.AppendLine($"<lastmod>{modif}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
        }
        foreach (var org in (await PuRepo.GetPlatyForYearsAsync(PuRepo.MinYear,PuRepo.DefaultYear)))
        {
            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>https://platyuredniku.hlidacstatu.cz/detail/{System.Security.SecurityElement.Escape(org.DS)}</loc>");
            sb.AppendLine($"<lastmod>{modif}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
            foreach (var item in org.Platy)
            {
                sb.AppendLine("<url>");
                sb.AppendLine($"<loc>https://platyuredniku.hlidacstatu.cz/Plat/{System.Security.SecurityElement.Escape(item.Id.ToString())}</loc>");
                sb.AppendLine($"<lastmod>{modif}</lastmod>");
                sb.AppendLine($"<priority>0.60</priority>");
                sb.AppendLine($"</url>");
            }
        }


        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml");
    }
    
    public IActionResult Error()
    {
        return View();
    }

    public IActionResult StatusCode(int code)
    {
        if (code == 404)
        {
            return View("NotFound");
        }
        else if (code >= 500)
        {
            return View("ServerError");
        }
        return View("Error");
    }
}