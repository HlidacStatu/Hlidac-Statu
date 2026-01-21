using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Extensions
{
    public static class SponzoringExtension
    {
        public static async Task<string> JmenoPrijemceAsync(this Sponzoring sponzoring)
        {
            bool prijemceJeFirma = !string.IsNullOrWhiteSpace(sponzoring.IcoPrijemce);
            if (prijemceJeFirma)
            {
                var zkratkyStran = await StaticData.GetZkratkyPolitickychStranAsync();
                return zkratkyStran.TryGetValue(sponzoring.IcoPrijemce, out string nazev)
                    ? nazev
                    : await FirmaCache.GetJmenoAsync(sponzoring.IcoPrijemce);
            }

            bool prijemcejeOsoba = sponzoring.OsobaIdPrijemce != null && sponzoring.OsobaIdPrijemce > 0;
            if (prijemcejeOsoba)
            {
                return (await OsobaCache.GetPersonByIdAsync(sponzoring.OsobaIdPrijemce.Value)).FullName();
            }

            //todo: log corrupted data
            return "";
        }

        public static async Task<string> ToHtmlAsync(this Sponzoring sponzoring, string itemTemplate = "{0}")
        {
            string kohoSponzoroval = await sponzoring.JmenoPrijemceAsync();
            if (string.IsNullOrWhiteSpace(kohoSponzoroval))
            {
                // nevime
                return "";
            }

            var kdySponzoroval = sponzoring.DarovanoDne.HasValue
                ? $"v roce {sponzoring.DarovanoDne?.Year}"
                : "v neznámém datu";

            var hodnotaDaruKc = Util.RenderData.NicePrice(sponzoring.Hodnota ?? 0, html: true);
            var dar = (sponzoring.Typ == (int)Sponzoring.TypDaru.FinancniDar)
                ? $"částkou {hodnotaDaruKc}"
                : $"nepeněžním darem ({sponzoring.Popis}) v hodnotě {hodnotaDaruKc}";
            var zdroj = string.IsNullOrWhiteSpace(sponzoring.Zdroj)
                ? ""
                : $"(<a target=\"_blank\" href=\"{sponzoring.Zdroj}\"><span class=\"glyphicon glyphicon-link\" aria-hidden=\"true\"></span>zdroj</a>)";

            if (sponzoring.Typ == (int)Sponzoring.TypDaru.DarFirmy)
                return string.Format(itemTemplate,
                    $"Člen statut. orgánu ve firmě {await FirmaCache.GetJmenoAsync(sponzoring.IcoDarce)} sponzorující {kohoSponzoroval} {kdySponzoroval}, hodnota daru {hodnotaDaruKc}");

            return string.Format(itemTemplate, $"Sponzor {kohoSponzoroval} {kdySponzoroval} {dar} {zdroj}");
        }


    }
}