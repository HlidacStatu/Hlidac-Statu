using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.XLib.Render;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;

namespace HlidacStatu.Web.Framework
{
    public static class LocalHtmlExtensions
    {

        public static IHtmlContent SubjektTypTrojice(this IHtmlHelper self, Firma firma, string htmlProOVM, string htmlProStatniFirmu, string htmlProSoukromou)
        {
            if (firma == null)
                return self.Raw("");
            if (firma.JsemOVM())
                return self.Raw(htmlProOVM);
            else if (firma.JsemStatniFirma())
                return self.Raw(htmlProStatniFirmu);
            else
                return self.Raw(htmlProSoukromou);
        }


        public static IHtmlContent RenderVazby(this IHtmlHelper self, HlidacStatu.DS.Graphs.Graph.Edge[] vazbyToRender)
        {
            if (vazbyToRender == null)
            {
                return self.Raw("");
            }
            if (vazbyToRender.Count() == 0)
            {
                return self.Raw("");
            }

            if (vazbyToRender.Count() == 1)
                return self.Raw($"{vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}");
            else
            {
                return self.Raw("Nepřímá vazba přes:<br/><small>"
                    + $"{vazbyToRender.First().From?.PrintName()} {vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}"
                    + $" → "
                    + vazbyToRender.Skip(1).Select(m => m.Descr + " v " + m.To.PrintName()).Aggregate((f, s) => f + " → " + s)
                    + "</small>"
                    );
            }
        }

    }
}