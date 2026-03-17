using HlidacStatu.RegistrVozidel.Models;
using HlidacStatu.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
            "PORSCHE", "JAGUAR", "LOTUS", "TESLA", "LEXUS",
            "INFINITI", "ALFA ROMEO", "CADILLAC", "LINCOLN", "GENESIS",
            "POLESTAR", "RIMAC", "MORGAN", "MAYBACH", "LAND ROVER",
            "HUMMER", "DS", "LANCIA", "CUPRA", "VOLVO"
        };

        private static readonly string VehicleCategoryFilter = "v.Kategorie_vozidla LIKE 'M1%'";

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

        public ActionResult VIN(string id)
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

        public async Task<ActionResult> NejsilnejsiAuta(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var sql = $@"
                SELECT TOP 50
                    v.PCV, v.Tovarni_znacka, v.Obchodni_oznaceni, v.Typ,
                    v.Max_vykon, v.Nejvyssi_rychlost, v.Palivo,
                    v.Datum_1_registrace_v_CR, v.Barva, v.logoslug, v.VIN,
                    v.Rok_vyroby, v.Zdvihovy_objem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE YEAR(v.Datum_1_registrace_v_CR) = @rok
                    AND v.Max_vykon IS NOT NULL
                    AND v.Max_vykon > 0
                    AND {VehicleCategoryFilter}
                ORDER BY v.Max_vykon DESC";

            var items = await ExecuteVehicleQueryAsync(sql, new[] { new SqlParameter("@rok", year) });
            var model = new VehicleYearReport
            {
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Report B: Top 50 fastest cars by year ====================

        public async Task<ActionResult> NejrychlejsiAuta(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var sql = $@"
                SELECT TOP 50
                    v.PCV, v.Tovarni_znacka, v.Obchodni_oznaceni, v.Typ,
                    v.Max_vykon, v.Nejvyssi_rychlost, v.Palivo,
                    v.Datum_1_registrace_v_CR, v.Barva, v.logoslug, v.VIN,
                    v.Rok_vyroby, v.Zdvihovy_objem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE YEAR(v.Datum_1_registrace_v_CR) = @rok
                    AND v.Nejvyssi_rychlost IS NOT NULL
                    AND v.Nejvyssi_rychlost > 0
                    AND {VehicleCategoryFilter}
                ORDER BY v.Nejvyssi_rychlost DESC";

            var items = await ExecuteVehicleQueryAsync(sql, new[] { new SqlParameter("@rok", year) });
            var model = new VehicleYearReport
            {
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Report C: Top 50 luxury cars by year ====================

        public async Task<ActionResult> LuxusniAuta(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var brandList = string.Join(", ", LuxuryBrands.Select(b => $"'{b}'"));
            var sql = $@"
                SELECT TOP 50
                    v.PCV, v.Tovarni_znacka, v.Obchodni_oznaceni, v.Typ,
                    v.Max_vykon, v.Nejvyssi_rychlost, v.Palivo,
                    v.Datum_1_registrace_v_CR, v.Barva, v.logoslug, v.VIN,
                    v.Rok_vyroby, v.Zdvihovy_objem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE YEAR(v.Datum_1_registrace_v_CR) = @rok
                    AND {VehicleCategoryFilter}
                    AND UPPER(v.Tovarni_znacka) IN ({brandList})
                ORDER BY v.Max_vykon DESC";

            var items = await ExecuteVehicleQueryAsync(sql, new[] { new SqlParameter("@rok", year) });
            var model = new VehicleYearReport
            {
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Report D: Newly registered cars (last month) ====================

        public async Task<ActionResult> NovaAuta()
        {
            var sql = $@"
                SELECT TOP 200
                    v.PCV, v.Tovarni_znacka, v.Obchodni_oznaceni, v.Typ,
                    v.Max_vykon, v.Nejvyssi_rychlost, v.Palivo,
                    v.Datum_1_registrace_v_CR, v.Barva, v.logoslug, v.VIN,
                    v.Rok_vyroby, v.Zdvihovy_objem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE v.Datum_1_registrace_v_CR >= DATEADD(MONTH, -1, CAST(GETDATE() AS DATE))
                    AND {VehicleCategoryFilter}
                ORDER BY v.Datum_1_registrace_v_CR DESC";

            var items = await ExecuteVehicleQueryAsync(sql);
            return View(items);
        }

        // ==================== Report E: Cars with expired technical inspection ====================

        public async Task<ActionResult> PropadlaSTK()
        {
            var sql = $@"
                SELECT TOP 200
                    v.PCV, v.Tovarni_znacka, v.Obchodni_oznaceni, v.Typ,
                    v.Max_vykon, v.Palivo,
                    v.Datum_1_registrace_v_CR, v.Barva, v.logoslug, v.VIN,
                    v.Rok_vyroby,
                    tp.Platnost_do
                FROM vypis_vozidel v WITH (NOLOCK)
                INNER JOIN technicke_prohlidky tp WITH (NOLOCK)
                    ON v.PCV = tp.PCV AND tp.Aktualni = 1
                WHERE tp.Platnost_do IS NOT NULL
                    AND tp.Platnost_do < CAST(GETDATE() AS DATE)
                    AND {VehicleCategoryFilter}
                ORDER BY tp.Platnost_do DESC";

            var items = await ExecuteVehicleQueryAsync(sql);
            return View(items);
        }

        // ==================== Additional Report: Most popular brands by year ====================

        public async Task<ActionResult> NejpopularnejsiZnacky(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var sql = $@"
                SELECT TOP 50
                    v.Tovarni_znacka,
                    MIN(v.logoslug) AS logoslug,
                    COUNT(*) AS Pocet
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE YEAR(v.Datum_1_registrace_v_CR) = @rok
                    AND {VehicleCategoryFilter}
                    AND v.Tovarni_znacka IS NOT NULL
                GROUP BY v.Tovarni_znacka
                ORDER BY Pocet DESC";

            var items = new List<BrandStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", year));

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

            var model = new BrandYearReport
            {
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Additional Report: Electric and hybrid vehicles trend ====================

        public async Task<ActionResult> Elektromobily()
        {
            var sql = $@"
                SELECT
                    YEAR(v.Datum_1_registrace_v_CR) AS Rok,
                    SUM(CASE WHEN v.Plne_elektricke_vozidlo = 1 THEN 1 ELSE 0 END) AS Elektricka,
                    SUM(CASE WHEN v.Hybridni_vozidlo = 1 THEN 1 ELSE 0 END) AS Hybridni,
                    COUNT(*) AS Celkem
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE v.Datum_1_registrace_v_CR IS NOT NULL
                    AND {VehicleCategoryFilter}
                    AND YEAR(v.Datum_1_registrace_v_CR) >= 2010
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

            return View(items);
        }

        // ==================== Additional Report: Import statistics by country ====================

        public async Task<ActionResult> DovozVozidel(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var sql = $@"
                SELECT TOP 30
                    d.Stat,
                    COUNT(*) AS Pocet
                FROM vozidla_dovoz d WITH (NOLOCK)
                INNER JOIN vypis_vozidel v WITH (NOLOCK) ON d.PCV = v.PCV
                WHERE YEAR(d.Datum_dovozu) = @rok
                    AND {VehicleCategoryFilter}
                    AND d.Stat IS NOT NULL
                    AND d.Stat <> ''
                GROUP BY d.Stat
                ORDER BY Pocet DESC";

            var items = new List<ImportStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", year));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ImportStatItem
                {
                    Country = await reader.IsDBNullAsync(0) ? null : reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }

            var model = new ImportYearReport
            {
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items
            };
            return View(model);
        }

        // ==================== Additional Report: Average vehicle age by brand ====================

        public async Task<ActionResult> StariVozidel()
        {
            var sql = $@"
                SELECT TOP 30
                    v.Tovarni_znacka,
                    MIN(v.logoslug) AS logoslug,
                    AVG(CAST(DATEDIFF(YEAR, v.Datum_1_registrace, GETDATE()) AS FLOAT)) AS PrumernyVek,
                    COUNT(*) AS Pocet
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE v.Datum_1_registrace IS NOT NULL
                    AND {VehicleCategoryFilter}
                    AND v.Tovarni_znacka IS NOT NULL
                GROUP BY v.Tovarni_znacka
                HAVING COUNT(*) >= 100
                ORDER BY PrumernyVek ASC";

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
                    Brand = await reader.IsDBNullAsync(0) ? null : reader.GetString(0),
                    LogoSlug = await reader.IsDBNullAsync(1) ? null : reader.GetString(1),
                    AverageAge = await reader.IsDBNullAsync(2) ? 0 : reader.GetDouble(2),
                    Count = reader.GetInt32(3)
                });
            }

            return View(items);
        }

        // ==================== Additional Report: Fuel type distribution by year ====================

        public async Task<ActionResult> RozdeleniPaliv(int? rok)
        {
            int year = rok ?? DateTime.Now.Year;
            var sql = $@"
                SELECT
                    v.Palivo,
                    COUNT(*) AS Pocet
                FROM vypis_vozidel v WITH (NOLOCK)
                WHERE YEAR(v.Datum_1_registrace_v_CR) = @rok
                    AND {VehicleCategoryFilter}
                    AND v.Palivo IS NOT NULL
                    AND v.Palivo <> ''
                GROUP BY v.Palivo
                ORDER BY Pocet DESC";

            var items = new List<FuelStatItem>();
            int totalCount = 0;
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@rok", year));

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
                SelectedYear = year,
                AvailableYears = GetAvailableYears(),
                Items = items,
                TotalCount = totalCount
            };
            return View(model);
        }

        // ==================== SQL Helpers ====================

        private static async Task<List<VehicleStatItem>> ExecuteVehicleQueryAsync(
            string sql, SqlParameter[] parameters = null)
        {
            var items = new List<VehicleStatItem>();
            await using var connection = new SqlConnection(RegistrVozidelCnnStr);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = CommandTimeoutSeconds;
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new VehicleStatItem();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i)) continue;
                    switch (reader.GetName(i))
                    {
                        case "PCV":
                            item.PCV = reader.GetString(i);
                            break;
                        case "Tovarni_znacka":
                            item.Brand = reader.GetString(i);
                            break;
                        case "Obchodni_oznaceni":
                            item.Model = reader.GetString(i);
                            break;
                        case "Typ":
                            item.Type = reader.GetString(i);
                            break;
                        case "Max_vykon":
                            item.PowerKw = reader.GetDecimal(i);
                            break;
                        case "Nejvyssi_rychlost":
                            item.TopSpeedKmh = reader.GetDecimal(i);
                            break;
                        case "Palivo":
                            item.Fuel = reader.GetString(i);
                            break;
                        case "Datum_1_registrace_v_CR":
                            item.RegistrationDate = reader.GetDateTime(i);
                            break;
                        case "Barva":
                            item.Color = reader.GetString(i);
                            break;
                        case "logoslug":
                            item.LogoSlug = reader.GetString(i);
                            break;
                        case "VIN":
                            item.VIN = reader.GetString(i);
                            break;
                        case "Rok_vyroby":
                            item.YearOfManufacture = reader.GetInt32(i);
                            break;
                        case "Zdvihovy_objem":
                            item.EngineDisplacement = reader.GetString(i);
                            break;
                        case "Platnost_do":
                            item.StkExpiryDate = reader.GetDateTime(i);
                            break;
                    }
                }
                items.Add(item);
            }
            return items;
        }
    }
}
