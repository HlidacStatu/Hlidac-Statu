using HlidacStatu.Entities;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.RegistrVozidel;
using HlidacStatu.RegistrVozidel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Controllers
{
    public class VozidlaController : Controller
    {
        private static readonly string RegistrVozidelCnnStr =
            Devmasters.Config.GetWebConfigValue("RegistrVozidelCnnStr");

        private static readonly int MinYear = 2000;
        private static readonly int CommandTimeoutSeconds = 120;

        private static readonly string[] LuxuryBrands = new[]
        {
            "ROLLS-ROYCE", "BENTLEY", "FERRARI", "LAMBORGHINI", "BUGATTI",
            "PAGANI", "KOENIGSEGG", "MCLAREN", "ASTON MARTIN", "MASERATI",
            "PORSCHE", "JAGUAR", "LOTUS", 
            "TESLA", //?? 
            "LEXUS","INFINITI", "ALFA ROMEO", "CADILLAC", "LINCOLN", "GENESIS",
            "POLESTAR", "RIMAC", "MORGAN", "MAYBACH", "LAND ROVER",
            "HUMMER", "DS", "LANCIA", "CUPRA", "VOLVO"
        };

        private static readonly string VehicleCategoryFilter = "v.Kategorie_vozidla LIKE 'M1%'";

        private static string StatniOnlyFilter(bool statniOnly, string pcvColumn = "v.PCV")
        {
            return statniOnly
                ? $"AND EXISTS (SELECT 1 FROM vlastnik_provozovatel_vozidla vl WITH (NOLOCK) INNER JOIN firmy.dbo.Firma f WITH (NOLOCK) ON f.ICO = vl.ICO AND f.typ >= 9 WHERE vl.PCV = {pcvColumn} and vl.Aktualni = 1)"
                : "";
        }

        private static List<int> GetAvailableYears()
        {
            var years = new List<int>();
            for (int y = DateTime.Now.Year; y >= MinYear; y--)
                years.Add(y);
            return years;
        }

        // ==================== Existing actions ====================

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult VIN(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            id = id.ToUpper().Trim();
            using var db = new dbCtx();
            var vv = db.VypisVozidel
                    .AsNoTracking()
                    .Select(m => m)
                    .Where(v => v.Vin == id)
                    .FirstOrDefault();

            return View(vv);
        }

        // ==================== Statistics landing page ====================

        public ActionResult Statistiky()
        {
            return View();
        }

        // ==================== Report A: Top 50 most powerful cars by year ====================

        public async Task<ActionResult> AutaPodlePohonu(int? refresh)
        {
            var model = await Repo.Cached.GetPoctyVozidelPodlePohonuAsync(refresh == 1);
            return View(model);
        }


        public async Task<ActionResult> AutaPodleVykonu(int? refresh)
        {
            List<(int min_vykon, int max_vykon, int stat_count, int soukr_count)> model = new();
            foreach (var interval in HlidacStatu.RegistrVozidel.Repo.PoctyVozidelPodleVykonuIntervals)
                model.Add( await HlidacStatu.RegistrVozidel.Repo.Cached.GetPoctyVozidelPodleVykonuAsync(interval.Item1, interval.Item2, refresh==1));

            return View(model);
        }

        // ==================== Report B: Top 50 fastest cars by year ====================


        // ==================== Report C: Top 50 luxury cars by year ====================

        public async Task<ActionResult> LuxusniAuta(string? id, int? rok, bool statniOnly = false)
        {
            var brandList = string.Join(", ", LuxuryBrands.Select(b => $"'{b}'"));
            var sql = $@"
                SELECT DISTINCT TOP 50
    p.Pcv,
    p.Ico,
    p.Typ_Subjektu        AS Typ_subjekt,
    p.Vztah_K_Vozidlu      AS Vztah_k_vozidlu,
    p.Aktualni,
    p.Datum_Od,
    p.Datum_Do,
    -- from VypisVozidel (v)
    v.Vin               AS VIN,
    v.Palivo,
    v.Kategorie_Vozidla  AS Kategorie_vozidla,
    v.Tovarni_Znacka     AS Tovarni_znacka,
    v.Obchodni_Oznaceni   AS Model,
    v.Rok_Vyroby         AS Rok_vyroby,
    v.Datum_1_registrace   AS Datum_1_registrace,
    v.Datum_1_registrace_v_CR AS Datum_1_registrace_v_CR,
    v.Zdvihovy_Objem     AS Zdvihovy_objem,
    v.Barva,
    v.Nejvyssi_Rychlost  AS Nejvyssi_rychlost,
    v.Plne_Elektricke_Vozidlo,
    v.Hybridni_Vozidlo,
    v.Stupen_Plneni_Emisni_Urovne AS Stupen_plneni_emisni_urovne,
    v.Provozni_Hmotnost,
    -- from TechnickeProhlidky (stk aggregate)
    stk.PosledniStk,
    stk.PlatnostStkMax   AS PlastnostStk,
    0 as pocet,
    v.Max_vykon
FROM Vlastnik_Provozovatel_Vozidla p
INNER JOIN Vypis_Vozidel v ON p.Pcv = v.Pcv
    AND v.PCV in (
     select top 200 pcv from vypis_vozidel 
        WHERE 
            ((Datum_1_registrace_v_CR >= DATEFROMPARTS(@rok, 1, 1) 
                   AND Datum_1_registrace_v_CR < DATEFROMPARTS(@rok + 1, 1, 1))
                   OR @rok IS NULL)

                AND UPPER(Tovarni_znacka) IN ({brandList})
            ORDER BY Max_vykon DESC
     )

INNER JOIN (
    SELECT
        Pcv,
        MAX(Platnost_Od) AS PosledniStk,
        MAX(Platnost_Do) AS PlatnostStkMax
    FROM Technicke_Prohlidky
    GROUP BY Pcv
) stk ON p.Pcv = stk.Pcv

    inner join firmy.dbo.Firma f  with (nolock) on f.ICO = p.ICO and f.typ>={(statniOnly ? "9" : "0")}
            and (f.ico = '{id}' or '{id}' is null or '{id}'='') and p.Aktualni = 1
    ORDER BY v.Max_vykon DESC
OPTION (RECOMPILE)
";

            var items = await Repo.ExecuteVehicleQueryAsync(sql, new[] { new SqlParameter("@rok", (object)rok ?? DBNull.Value) });
            ViewBag.StatniOnly = statniOnly;
            var model = new VehicleYearReport
            {
                SelectedYear = rok,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Report D: Newly registered cars (last month) ====================

        [HlidacOutputCache(durationInSeconds: 10 * 60 * 60)]
        public async Task<ActionResult> NovaAuta(string? id, int mesice=12, int top = 150)
        {
            var fromD = DateTime.Now.AddMonths(-1*mesice);
            IEnumerable<Tuple<string, int, Dictionary<string, int>>> items = await Repo.NoveVozyPerIcoAsync(fromD, id, top);
            ViewBag.Mesice = mesice;
            return View(items);
        }

        // ==================== Report E: Cars with expired technical inspection ====================

        [HlidacOutputCache(durationInSeconds: 10*60*60)]
        public async Task<ActionResult> PropadleSTK()
        {
            
            IEnumerable<Tuple<string, int>> items = await Repo.Cached.GetPropadleSTKperICOTopAsync();
            return View(items);
        }

        // ==================== Additional Report: Most popular brands by year ====================

        public async Task<ActionResult> NejpopularnejsiZnacky(int? rok, bool statniOnly = false)
        {
            var sql = $@"
                SELECT TOP 50
                    v.Tovarni_znacka,
                    MIN(v.logoslug) AS logoslug,
                    COUNT(*) AS Pocet
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE 
                    ((v.Datum_1_registrace_v_CR >= DATEFROMPARTS(@rok, 1, 1) 
                           AND v.Datum_1_registrace_v_CR < DATEFROMPARTS(@rok + 1, 1, 1))
                           OR @rok IS NULL)
                    AND NOT EXISTS (
                            SELECT 1 FROM vozidla_vyrazena_z_provozu vzp with (nolock)
                            WHERE vzp.PCV = v.PCV
                        )

                    AND v.logoslug IS NOT NULL
                    {StatniOnlyFilter(statniOnly)}
                GROUP BY v.Tovarni_znacka
                ORDER BY Pocet DESC
OPTION (RECOMPILE)";

            var items = new List<BrandStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", (object)rok ?? DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new BrandStatItem
                {
                    Brand = await reader.IsDBNullAsync(0) ? null : reader.GetString(0),
                    LogoSlug = await reader.IsDBNullAsync(1) ? null : reader.GetString(1),
                    Count = reader.GetInt32(2)
                });
            }

            ViewBag.StatniOnly = statniOnly;
            var model = new BrandYearReport
            {
                SelectedYear = rok,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Additional Report: Electric and hybrid vehicles trend ====================

        public async Task<ActionResult> Elektromobily(bool statniOnly = false)
        {
            var sql = $@"
                SELECT
                    YEAR(v.Datum_1_registrace_v_CR) AS Rok,
                    SUM(CASE WHEN v.Plne_elektricke_vozidlo = 1 THEN 1 ELSE 0 END) AS Elektricka,
                    SUM(CASE WHEN v.Hybridni_vozidlo = 1 THEN 1 ELSE 0 END) AS Hybridni,
                    COUNT(*) AS Celkem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE v.Datum_1_registrace_v_CR IS NOT NULL                    
                    {StatniOnlyFilter(statniOnly)}
                GROUP BY YEAR(v.Datum_1_registrace_v_CR)
                ORDER BY Rok";

            var items = new List<EvTrendItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new EvTrendItem
                {
                    Year = reader.GetInt32(0),
                    ElectricCount = reader.GetInt32(1),
                    HybridCount = reader.GetInt32(2),
                    TotalCount = reader.GetInt32(3)
                });
            }

            ViewBag.StatniOnly = statniOnly;
            return View(items);
        }

        // ==================== Additional Report: Import statistics by country ====================

        public async Task<ActionResult> DovozVozidel(int? rok, bool statniOnly = false)
        {
            var sql = $@"
                SELECT TOP 30
                    d.Stat,
                    COUNT(*) AS Pocet
                FROM vozidla_dovoz d WITH (NOLOCK)
                INNER JOIN vypis_vozidel v WITH (NOLOCK) ON d.PCV = v.PCV
                WHERE (
                    (v.Datum_dovozu >= DATEFROMPARTS(@rok, 1, 1) 
                       AND v.Datum_dovozu < DATEFROMPARTS(@rok + 1, 1, 1))
                   OR @rok IS NULL)
                    
                    AND d.Stat IS NOT NULL
                    AND d.Stat <> ''
                    {StatniOnlyFilter(statniOnly)}
                GROUP BY d.Stat
                ORDER BY Pocet DESC
";

            var items = new List<ImportStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", (object)rok ?? DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ImportStatItem
                {
                    Country = await reader.IsDBNullAsync(0) ? null : reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }

            ViewBag.StatniOnly = statniOnly;
            var model = new ImportYearReport
            {
                SelectedYear = rok,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Additional Report: Average vehicle age by brand ====================

        public async Task<ActionResult> StariVozidel(bool statniOnly = false)
        {
            var sql = $@"
SELECT TOP 100
    v.logoslug,
    AVG(CAST(DATEDIFF(YEAR, v.Datum_1_registrace, GETDATE()) AS FLOAT)) AS PrumernyVek,
    COUNT(*) AS Pocet
FROM vypis_vozidel v WITH (NOLOCK)

WHERE v.Datum_1_registrace IS NOT NULL
    AND v.logoslug IS NOT NULL
    and (RM_zaniku = '' or RM_zaniku is null)
    AND NOT EXISTS (
            SELECT 1 FROM vozidla_vyrazena_z_provozu vzp with (nolock)
            WHERE vzp.PCV = v.PCV
        )
    and Datum_1_registrace IS NOT NULL 
    AND Tovarni_znacka IS NOT NULL
    {StatniOnlyFilter(statniOnly)}
GROUP BY v.logoslug
HAVING COUNT(*) >= 100
ORDER BY PrumernyVek desc

";

            var items = new List<AgeStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new AgeStatItem
                {
                    Brand= await reader.IsDBNullAsync(1) ? null : reader.GetString(0),
                    LogoSlug = await reader.IsDBNullAsync(1) ? null : reader.GetString(0),
                    AverageAge = await reader.IsDBNullAsync(2) ? 0 : reader.GetDouble(1),
                    Count = reader.GetInt32(2)
                });
            }

            ViewBag.StatniOnly = statniOnly;
            return View(items);
        }

        // ==================== Additional Report: Fuel type distribution by year ====================

        public async Task<ActionResult> RozdeleniPaliv(int? rok, bool statniOnly = false)
        {
            var sql = $@"
                SELECT
                    v.Palivo_Kategorie,
                    COUNT(*) AS Pocet
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE 
                    ((v.Datum_1_registrace_v_CR >= DATEFROMPARTS(@rok, 1, 1)
                          AND v.Datum_1_registrace_v_CR < DATEFROMPARTS(@rok + 1, 1, 1))
                       OR @rok IS NULL)
                    AND v.Palivo_Kategorie IS NOT NULL
                    AND v.Palivo_Kategorie <> ''
                    AND NOT EXISTS (
                            SELECT 1 FROM vozidla_vyrazena_z_provozu vzp with (nolock)
                            WHERE vzp.PCV = v.PCV
                        )
                    {StatniOnlyFilter(statniOnly)}
                GROUP BY v.Palivo_Kategorie
                ORDER BY Pocet DESC
OPTION (RECOMPILE) ";

            var items = new List<FuelStatItem>();
            int totalCount = 0;
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", (object)rok ?? DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new FuelStatItem
                {
                    FuelType = await reader.IsDBNullAsync(0) ? null : reader.GetString(0),
                    Count = reader.GetInt32(1)
                };
                totalCount += item.Count;
                items.Add(item);
            }

            var model = new FuelYearReport
            {
                SelectedYear = rok,
                AvailableYears = GetAvailableYears(),
                Items = items,
                TotalCount = totalCount
            };
            return View(model);
        }


    }
}
