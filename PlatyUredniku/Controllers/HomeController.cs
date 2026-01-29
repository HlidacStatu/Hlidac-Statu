
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Linq;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.Repositories.Cache;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index()
    {
        return View("Index");
    }

    [HlidacOutputCache(48 * 60 * 60, "*")]
    public async Task<IActionResult> Organizace(string id)
    {
        string[] ds = null;
        PuOrganizace detail = null;
        if (HlidacStatu.Util.DataValidators.CheckCZICO(id))
        {
            //ico
            var f = await FirmaCache.GetAsync(id);
            if (f?.Valid == true)
                ds = f.DatovaSchranka;

        }
        else
        {
            ds = new[] { id };
        }

        if (ds?.Length > 0)
        {
            await using var db = new DbEntities();

            var f1 = await PuRepo.GetFullDetailAsync(ds);
            var f2 = await PpRepo.GetOrganizaceFullDetailAsync(ds);
            detail = f1;
            if (detail == null)
            {
                detail = f2;
            }
            if (detail != null)
                detail.PrijmyPolitiku = f2?.PrijmyPolitiku;
        }

        if (detail == null)
            return Redirect("/");


        if (detail.Platy?.Count > 0 && detail.PrijmyPolitiku?.Count > 0)
        {
            return View(detail);
        }

        if (detail.Platy?.Count > 0)
            return View(detail);
        else
            return RedirectToAction("organizace", "Politici", new { id = detail.DS });
    }

    public IActionResult OpenData()
    {
        ViewBag.Title = $"Open data";

        return View();
    }


    [HlidacOutputCache(3600 * 24 * 7)]
    public async Task<IActionResult> SiteMap()
    {
        var db = new DbEntities();

        DateTime modifDatePu = db.PuPlaty.Where(m => m.DateModified != null).Max(m => m.DateModified) ?? DateTime.Now;
        DateTime modifDatePP = db.PpPrijmy.Where(m => m.DateModified != null).Max(m => m.DateModified) ?? DateTime.Now;
        DateTime modifDate = new DateTime(Math.Max(modifDatePu.Ticks, modifDatePP.Ticks));
        string modif = $"{modifDate:yyyy-MM-dd}";
        //DateTime.Now.Date.ToString("yyyy-MM-dd") + "T09:00:00+00:00";
        StringBuilder sb = new StringBuilder(1024 * 20);
        sb.AppendLine(@$"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.sitemaps.org/schemas/sitemap/0.9
            http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"">
    <url>
        <loc>https://platy.hlidacstatu.cz/</loc>
        <lastmod>{modif}</lastmod>
        <priority>1.00</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/Texty/OProjektu</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/Texty/PlatyStatnichZamestnancu</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/Texty/Nejvyssi</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>

    <url>
        <loc>https://platy.hlidacstatu.cz/DlePlatu/1</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/DlePlatu/2</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/DlePlatu/3</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/DlePlatu/4</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/analyzy</loc>
        <lastmod>{modif}</lastmod>
        <priority>0.80</priority>
    </url>
    <url>
        <loc>https://platy.hlidacstatu.cz/analyza/</loc>
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
            sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/Urednici/analyza/{System.Security.SecurityElement.Escape(item)}</loc>");
            sb.AppendLine($"<lastmod>{modif}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
        }

        foreach (var item in PuRepo.MainTags)
        {
            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/Urednici/Oblast/{System.Security.SecurityElement.Escape(PuOrganizaceTag.NormalizeTag(item))}</loc>");
            sb.AppendLine($"<lastmod>{modif}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
        }
        foreach (PuOrganizace? org in (await PuRepo.GetPlatyForYearsAsync(PuRepo.MinYear, PuRepo.DefaultYear)))
        {
            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/Urednici/detail/{System.Security.SecurityElement.Escape(org.DS)}</loc>");
            sb.AppendLine($"<lastmod>{(org.Platy.Max(m => m.DateModified) ?? modifDate):yyyy-MM-dd}</lastmod>");
            sb.AppendLine($"<priority>0.80</priority>");
            sb.AppendLine($"</url>");
            foreach (var item in org.Platy)
            {
                sb.AppendLine("<url>");
                sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/Urednici/Plat/{System.Security.SecurityElement.Escape(item.Id.ToString())}</loc>");
                sb.AppendLine($"<lastmod>{(item.DateModified ?? modifDate):yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"<priority>0.60</priority>");
                sb.AppendLine($"</url>");
            }
        }
        if (Devmasters.Config.GetWebConfigValue("ShowPrijmyPolitiku") == "true")
        {
            foreach (var item in new string[]{
            "politici/","politici/reporty","politici/vsechnyorganizace" }
)
            {
                sb.AppendLine("<url>");
                sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/{System.Security.SecurityElement.Escape(item)}</loc>");
                sb.AppendLine($"<lastmod>{modif}</lastmod>");
                sb.AppendLine($"<priority>0.80</priority>");
                sb.AppendLine($"</url>");
            }
            var roky = await PpRepo.GetRokyPotvrzenePlatyAsync(db);
            foreach (var rok in roky)
            {
                Dictionary<string, PpPrijem[]> platy = await PpRepo.GetPrijmyGroupedByNameIdAsync(rok);

                foreach (var kv in platy)
                {
                    sb.AppendLine("<url>");
                    sb.AppendLine($"<loc>https://platy.hlidacstatu.cz/Politici/politik/{kv.Key}</loc>");
                    sb.AppendLine($"<lastmod>{(kv.Value.Max(m => m.DateModified) ?? modifDate):yyyy-MM-dd}</lastmod>");
                    sb.AppendLine($"<priority>1.00</priority>");
                    sb.AppendLine($"</url>");
                }
            }
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml");
    }

    public IActionResult
        Error()
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