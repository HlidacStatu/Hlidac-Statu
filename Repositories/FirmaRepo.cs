using Devmasters;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Data.External.DatoveSchrankyOpenData;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public static string[] IgnoredIcos = Config
            .GetWebConfigValue("DontIndexFirmy")
            .Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.ToLower())
            .ToArray();

        private static readonly ILogger _logger = Log.ForContext(typeof(FirmaRepo));

        public static async Task SaveAsync(Firma firma)
        {
            try
            {
                await using DbEntities db = new DbEntities();
                if (firma.Jmeno.Length > 500)
                {
                    firma.Jmeno = firma.Jmeno.Substring(0, 500);
                }
                if (firma.JmenoAscii.Length > 500)
                {
                    firma.JmenoAscii = firma.JmenoAscii.Substring(0, 500);
                }

                bool existsInDb = await db.Firma.Where(m => m.ICO == firma.ICO).Select(m => m.ICO).FirstOrDefaultAsync() != null;
                db.Firma.Attach(firma);
                if (existsInDb)
                {
                    db.Entry(firma).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                else
                    db.Entry(firma).State = Microsoft.EntityFrameworkCore.EntityState.Added;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Saving EntityFr firma {firma.ICO}");
                }

                if (firma.NACE != null)
                {
                    _= await db.Database.ExecuteSqlInterpolatedAsync($"delete from firma_NACE where ico={firma.ICO}");
                    foreach (var nace in firma.NACE.Distinct())
                    {
                        _= await db.Database.ExecuteSqlInterpolatedAsync($"insert into firma_NACE (ico, nace) values ({firma.ICO}, {nace})");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Saving firma {firma.ICO}");
            }
        }

        public static async Task<Firma> FromDSAsync(string ds, bool getMissingFromExternal = false)
        {
            Firma f = await FromDSAsync(ds);
            if (!(f?.Valid == true) && getMissingFromExternal)
            {
                var d = ISDS.GetSubjektyForDS(ds);
                if (d != null)
                {
                    return await FromIcoAsync(d.ICO, getMissingFromExternal);
                }
            }

            return f;
        }

        public static async Task<Firma> FromNameAsync(string jmeno, bool getMissingFromExternal = false, bool wildcards = false)
        {
            Firma f = await FromNameFirstItemAsync(jmeno, wildcards);
            if (f?.Valid == true)
                return f;

            else if (getMissingFromExternal)
            {
                f = Merk.FromName(jmeno);
                if (f?.Valid == true)
                {
                    f.RefreshDS();
                    await SaveAsync(f);
                }
 
                return f;
            }
            else
            {
                return null;
            }
        }
        

        public static async Task<Firma> FromIcoExtAsync(string ico, bool getMissingFromExternal = false, bool loadAllData = true)
        {
            Firma f = await FromIcoAsync(ico, loadAllData);

            if (f?.Valid == true)
                return f;

            else if (getMissingFromExternal)
            {
                f = Merk.FromIco(ico);
                if (f?.Valid == true)
                    return f;

                if (!(f?.Valid == true)) //try firmo
                {
                    f = await RZP.FromIcoAsync(ico);
                }

                if (f == null)
                    return null;

                f.RefreshDS();
                await SaveAsync(f);
                return f;
            }
            else
            {
                return null;
            }
        }

        public static async Task<Dictionary<string, string[]>> GetNazvyOnlyAsciiAsync()
        {
            Dictionary<string, string[]> res = new ();

            await using DbEntities db = new DbEntities();
            var firmy = await db.Firma
                .Select(f => new { f.ICO, f.Jmeno })
                .AsNoTracking()
                .ToListAsync();

            foreach (var firma in firmy)
            {
                string ico = firma.ICO?.Trim();
                string name = firma.Jmeno?.Trim();

                if (string.IsNullOrEmpty(ico) || string.IsNullOrEmpty(name))
                    continue;

                if (ico.IsNumeric())
                {
                    ico = Util.ParseTools.NormalizeIco(ico);
                    var jmenoa = Firma.JmenoBezKoncovky(name).RemoveDiacritics()
                        .Trim().ToLower();

                    if (!res.ContainsKey(jmenoa))
                        res[jmenoa] = [ico];
                    else if (!res[jmenoa].Contains(ico))
                    {
                        var v = res[jmenoa];
                        res[jmenoa] = v.Union([ico]).ToArray();
                    }
                }
            }

            return res;
        }

        public static async Task<Firma> ToValidFirmaOnlyAsync(this Firma firma, DbEntities db)
        {
            if (firma is null || Util.DataValidators.CheckCZICO(firma.ICO) == false)
                return null;
            
            firma.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(firma.ICO);
            if(firma.Valid == false)
                return null;
            
            firma.DatovaSchranka = await db.Database
                .SqlQuery<string>($"select DatovaSchranka from firma_DS where ico={firma.ICO}")
                .ToArrayAsync();
            firma.NACE = await db.Database.SqlQuery<string>($"select NACE from firma_Nace where ico={firma.ICO}")
                .ToArrayAsync();
            return firma;
            
        }
        
        
        //při injectnutém db contextu se nesmí dělat paralelní operace
        public static async Task<Firma> FromIcoAsync(string ico, bool loadInvalidIco = false, DbEntities db = null)
        {
            if (Util.DataValidators.CheckCZICO(ico) == false && loadInvalidIco == false)
                return null;
        
            Firma f = new Firma();
            
            bool ownsContext = db == null;
            if (ownsContext)
                db = new DbEntities();
            try
            {
                f = await db.Firma.FirstOrDefaultAsync(m => m.ICO == ico);

                return await f.ToValidFirmaOnlyAsync(db);
            }
            finally
            {
                if (ownsContext && db != null)
                {
                    await db.DisposeAsync();
                }
            }
        }

        public static async Task<Firma> FromNameFirstItemAsync(string jmeno, bool wildcards = false)
        {
            IEnumerable<Firma> res = null;
            if (wildcards)
                res = await AllFromNameWildcardsAsync(jmeno);
            else
                res = await AllFromExactNameAsync(jmeno);
            return res.FirstOrDefault();

        }

        public static async Task<List<Firma>> AllFromExactNameAsync(string jmeno)
        {
            await using var db = new DbEntities();
            var res = await db.Firma
                .AsNoTracking()
                .Where(f => f.Jmeno == jmeno)
                .ToListAsync();

            List<Firma> result = [];
            if (res.Any())
            {
                foreach (var firma in res)
                {
                    var validFirma = await firma.ToValidFirmaOnlyAsync(db);
                    if (validFirma is not null)
                    {
                        result.Add(firma);
                    }
                }
            }
            
            return result;

        }

        public static async Task<List<(string ico, string jmeno, bool isFop)>> GetJmenoIcoAndFopTuplesAsync()
        {
            await using var db = new DbEntities();
            var res = await db.Firma
                .Where(f => EF.Functions.IsNumeric(f.ICO))
                .Select(f => new 
                {
                    ICO = Util.ParseTools.NormalizeIco(f.ICO.Trim()),
                    Jmeno = f.Jmeno.Trim(),
                    IsFOP = f.Kod_PF <= 110 || f.Kod_PF == null
                })
                .ToListAsync();
            
            return res.Select(r => (r.ICO, r.Jmeno, r.IsFOP)).ToList();
        }

        
        public static async Task<List<Firma>> AllFromNameWildcardsAsync(string jmeno)
        {
            await using var db = new DbEntities();
            var res = await db.Firma
                .AsNoTracking()
                .Where(f => EF.Functions.Like(f.Jmeno, jmeno))
                .ToListAsync();

            List<Firma> result = [];
            if (res.Any())
            {
                foreach (var firma in res)
                {
                    var validFirma = await firma.ToValidFirmaOnlyAsync(db);
                    if (validFirma is not null)
                    {
                        result.Add(firma);
                    }
                }
            }
            
            return result;
        }

        public static async Task<Firma> FromDSAsync(string ds)
        {
            await using var db = new DbEntities();
            var res = await (from fds in db.FirmaDs
                    join f in db.Firma on fds.Ico equals f.ICO
                    where fds.DatovaSchranka == ds
                    select f)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            
            return await res.ToValidFirmaOnlyAsync(db);
        }

        public static async Task<List<string>> AllIcoInRSAsync()
        {
            await using var db = new DbEntities();
            var res = await db.Firma.Where(f => f.IsInRS == 1).Select(f => f.ICO).AsNoTracking().ToListAsync();
            
            if (res.Any())
            {
                var allIcos = res
                    .Where(r => TextUtil.IsNumeric(r))
                    .ToList();

                return allIcos;
            }
            else
                return [];
        }

        public static async Task<string> NameFromIcoAsync(string ico, bool IcoIfNotFound = false)
        {
            if (string.IsNullOrEmpty(ico))
                return string.Empty;

            await using var db = new DbEntities();
            var res = await db.Firma.Where(f => f.ICO == ico).Select(f => f.Jmeno).AsNoTracking().FirstOrDefaultAsync();
            
            if (string.IsNullOrWhiteSpace(res))
            {
                if (IcoIfNotFound)
                    return "IČO:" + ico;
                else
                    return string.Empty;
            }
            else
                return res;
        }

        public static void RefreshDS(this Firma firma)
        {
            firma.DatovaSchranka = Lib.Data.External.DatoveSchrankyOpenData.ISDS.GetDatoveSchrankyForIco(firma.ICO);
        }
        
        
        // public static Firma FromIcoExt(int ico, bool getMissingFromExternal = false, bool loadAllData = true)
        // {
        //     return FromIcoExt(ico.ToString().PadLeft(8, '0'), getMissingFromExternal, loadAllData);
        // }
        
        // private static Firma FromDataRow(DataRow dr, bool skipDS_Nace = false)
        // {
        //     Firma f = new Firma();
        //     f.ICO = (string)dr["ico"];
        //     f.DIC = (string)Devmasters.DirectSql.IsNull(dr["dic"], string.Empty);
        //     f.Datum_Zapisu_OR = (DateTime?)Devmasters.DirectSql.IsNull(dr["datum_zapisu_or"], null);
        //     f.Stav_subjektu = (byte)Convert.ToInt32(Devmasters.DirectSql.IsNull(dr["Stav_subjektu"], 1));
        //     f.Status = Convert.ToInt32(Devmasters.DirectSql.IsNull(dr["Status"], 1));
        //     f.Jmeno = (string)Devmasters.DirectSql.IsNull(dr["jmeno"], string.Empty);
        //     f.JmenoAscii = (string)Devmasters.DirectSql.IsNull(dr["jmenoascii"], string.Empty);
        //     f.Kod_PF = (int?)Devmasters.DirectSql.IsNull(dr["Kod_PF"], null);
        //     f.VersionUpdate = (int)dr["VersionUpdate"];
        //     f.IsInRS = (short?)Devmasters.DirectSql.IsNull(dr["IsInRS"], null);
        //     f.KrajId = (string)Devmasters.DirectSql.IsNull(dr["krajid"], string.Empty);
        //     f.OkresId = (string)Devmasters.DirectSql.IsNull(dr["okresid"], string.Empty);
        //     f.Typ = (int?)Devmasters.DirectSql.IsNull(dr["typ"], null);
        //     f.ESA2010 = (string)Devmasters.DirectSql.IsNull(dr["Esa2010"], string.Empty);
        //
        //     if (skipDS_Nace == false)
        //     {
        //
        //         f.DatovaSchranka = await DirectDB.Instance.GetListAsync<string>("select DatovaSchranka from firma_DS where ico=@ico", CommandType.Text, new SqlParameter[] {
        //                 new SqlParameter("ico", f.ICO)
        //             })
        //             .ToArray();
        //
        //         f.NACE = await DirectDB.Instance.GetListAsync<string>("select NACE from firma_Nace where ico=@ico", CommandType.Text, new SqlParameter[] {
        //                 new SqlParameter("ico", f.ICO)
        //             })
        //             .ToArray();
        //     }
        //
        //     return f;
        //
        // }
        
        // public static IEnumerable<string> GetEntrepreneurIcos()
        // {
        //     var data = await DirectDB.Instance.GetListAsync<string>("select ico from firma where ISNUMERIC(ICO) = 1 AND Kod_PF <=110", CommandType.Text, null);
        //     return data;
        // }
        
        // public static void AddZahranicniFirma(string ico, string jmeno, string adresa)
        // {
        //     string sql = @"insert into firma(ico,dic,stav_subjektu, jmeno, jmenoascii, versionupdate, popis)
        //                         values(@ico,@dic,@stav,@jmeno,@jmenoascii,0,@adresa)";
        //
        //     await DirectDB.Instance.NoResultAsync(sql, CommandType.Text, new SqlParameter[] {
        //             new SqlParameter("ico", ico),
        //             new SqlParameter("dic", ico),
        //             new SqlParameter("stav", (int)1),
        //             new SqlParameter("jmeno", jmeno),
        //             new SqlParameter("jmenoascii", TextUtil.RemoveDiacritics(jmeno)),
        //             new SqlParameter("versionupdate", (long)0),
        //             new SqlParameter("adresa", TextUtil.ShortenText(adresa,100)),
        //             });
        //
        // }
        
    }


}