using Devmasters;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Data.External.DatoveSchrankyOpenData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
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

        public static void Save(Firma firma)
        {
            string sqlNACE = @"INSERT into firma_NACE(ico, nace) values(@ico,@nace)";
         
            try
            {
                using (DbEntities db = new DbEntities())
                {
                    if (firma.Jmeno.Length > 500)
                    {
                        firma.Jmeno = firma.Jmeno.Substring(0, 500);
                    }
                    if (firma.JmenoAscii.Length > 500)
                    {
                        firma.JmenoAscii = firma.JmenoAscii.Substring(0, 500);
                    }

                    bool existsInDb = db.Firma.AsQueryable().Where(m => m.ICO == firma.ICO).Select(m => m.ICO).FirstOrDefault() != null;
                    db.Firma.Attach(firma);
                    if (existsInDb)
                    {
                        db.Entry(firma).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                    else
                        db.Entry(firma).State = Microsoft.EntityFrameworkCore.EntityState.Added;

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Saving EntityFr firma {firma.ICO}");
                    }


                    /*                    if (firma.DatovaSchranka != null)
                                        {
                                            HlidacStatu.Connectors.DirectDB.Instance.NoResult("delete from firma_DS where ico=@ico",
                                                new IDataParameter[]
                                                {
                                                    new SqlParameter("ico", firma.ICO)
                                                });
                                            foreach (var ds in firma.DatovaSchranka.Distinct())
                                            {
                                                HlidacStatu.Connectors.DirectDB.Instance.NoResult(sqlDS, new IDataParameter[]
                                                {
                                                    new SqlParameter("ico", firma.ICO),
                                                    new SqlParameter("DatovaSchranka", ds),
                                                });
                                            }
                                        }*/

                    if (firma.NACE != null)
                    {
                        _= db.Database.ExecuteSqlInterpolated($"delete from firma_NACE where ico={firma.ICO}");
                        //HlidacStatu.Connectors.DirectDB.Instance.NoResult("delete from firma_NACE where ico=@ico",
                        //    new IDataParameter[]
                        //    {
                        //        new SqlParameter("ico", firma.ICO)
                        //    });
                        foreach (var nace in firma.NACE.Distinct())
                        {
                            _= db.Database.ExecuteSqlInterpolated($"insert into firma_NACE (ico, nace) values ({firma.ICO}, {nace})");
                            //HlidacStatu.Connectors.DirectDB.Instance.NoResult(sqlNACE, new IDataParameter[]
                            //{
                            //    new SqlParameter("ico", firma.ICO),
                            //    new SqlParameter("nace", nace),
                            //});
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Saving firma {firma.ICO}");
            }
        }

        public static Firma FromDS(string ds, bool getMissingFromExternal = false)
        {
            Firma f = FromDS(ds);
            if (!f.Valid && getMissingFromExternal)
            {
                var d = ISDS.GetSubjektyForDS(ds);
                if (d != null)
                {
                    return FromIco(d.ICO, getMissingFromExternal);
                }
            }

            return f;
        }

        public static Firma FromName(string jmeno, bool getMissingFromExternal = false, bool wildcards = false)
        {
            Firma f = FromNameFirstItem(jmeno, wildcards);
            if (f.Valid)
                return f;

            else if (getMissingFromExternal)
            {
                f = Merk.FromName(jmeno);
                if (f.Valid)
                    return f;

                if (f == null)
                    return Firma.NotFound;
                else if (f == Firma.NotFound || f == Firma.LoadError)
                    return f;

                f.RefreshDS();
                Save(f);
                return f;
            }
            else
            {
                return null;
            }
        }


        public static Firma FromIcoExt(int ico, bool getMissingFromExternal = false, bool loadAllData = true)
        {
            return FromIcoExt(ico.ToString().PadLeft(8, '0'), getMissingFromExternal, loadAllData);
        }

        public static Firma FromIcoExt(string ico, bool getMissingFromExternal = false, bool loadAllData = true)
        {
            Firma f = FromIco(ico, loadAllData);

            if (f.Valid)
                return f;

            else if (getMissingFromExternal)
            {
                f = Merk.FromIco(ico);
                if (f.Valid)
                    return f;

                if (!f.Valid) //try firmo
                {
                    f = RZP.FromIco(ico);
                }

                if (f == null)
                    return Firma.NotFound;
                else if (f == Firma.NotFound || f == Firma.LoadError)
                    return f;

                f.RefreshDS();
                Save(f);
                return f;
            }
            else
            {
                return Firma.NotFound;
            }
        }

        public static void AddZahranicniFirma(string ico, string jmeno, string adresa)
        {
            string sql = @"insert into firma(ico,dic,stav_subjektu, jmeno, jmenoascii, versionupdate, popis)
                                values(@ico,@dic,@stav,@jmeno,@jmenoascii,0,@adresa)";

            DirectDB.Instance.NoResult(sql, CommandType.Text, new IDataParameter[] {
                    new SqlParameter("ico", ico),
                    new SqlParameter("dic", ico),
                    new SqlParameter("stav", (int)1),
                    new SqlParameter("jmeno", jmeno),
                    new SqlParameter("jmenoascii", TextUtil.RemoveDiacritics(jmeno)),
                    new SqlParameter("versionupdate", (long)0),
                    new SqlParameter("adresa", TextUtil.ShortenText(adresa,100)),
                    });

        }

        public static Dictionary<string, string[]> GetNazvyOnlyAscii()
        {
            Dictionary<string, string[]> res = new ();

            using (DbEntities db = new DbEntities())
            {
                var firmy = db.Firma
                    .Select(f => new { f.ICO, f.Jmeno })
                    .AsNoTracking()
                    .ToList();

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
            }
            return res;
        }

        public static Firma FromIco(string ico, bool loadInvalidIco = false)
        {
            if (Util.DataValidators.CheckCZICO(ico) == false && loadInvalidIco == false)
                return Firma.NotFound;

            Firma f = new Firma();
            using (DbEntities db = new DbEntities())
            {

                f = db.Firma.FirstOrDefault(m => m.ICO == ico);
                if (f is null)
                    return Firma.NotFound;

                f.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(f.ICO);

                if (f != null)
                {
                    f.DatovaSchranka = db.Database.SqlQuery<string>($"select DatovaSchranka from firma_DS where ico={ico}")
                        .ToArray();

                    f.NACE = db.Database.SqlQuery<string>($"select NACE from firma_Nace where ico={ico}")
                        .ToArray();
                    
                    return f;
                }
                else
                    return Firma.NotFound;
            }
        }
        
        public static async Task<Firma> FromIcoAsync(string ico, bool loadInvalidIco = false)
        {
            if (Util.DataValidators.CheckCZICO(ico) == false && loadInvalidIco == false)
                return Firma.NotFound;
        
            Firma f = new Firma();
            await using DbEntities db = new DbEntities();
            
            f = await db.Firma.FirstOrDefaultAsync(m => m.ICO == ico);
            if (f is null)
                return Firma.NotFound;
        
            f.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(f.ICO);
        
            if (f != null)
            {
                f.DatovaSchranka = await db.Database.SqlQuery<string>($"select DatovaSchranka from firma_DS where ico={ico}")
                    .ToArrayAsync();
        
                f.NACE = await db.Database.SqlQuery<string>($"select NACE from firma_Nace where ico={ico}")
                    .ToArrayAsync();
                    
                return f;
            }
            else
                return Firma.NotFound;
        }

        public static Firma FromNameFirstItem(string jmeno, bool wildcards = false)
        {
            IEnumerable<Firma> res = null;
            if (wildcards)
                res = AllFromNameWildcards(jmeno);
            else
                res = AllFromExactName(jmeno);
            return res.FirstOrDefault(Firma.NotFound);

        }

        public static IEnumerable<Firma> AllFromExactName(string jmeno)
        {
            string sql = @"select ico from Firma where jmeno = @jmeno";

            var res = Connectors.DirectDB.Instance.GetList<string>(sql, param: new IDataParameter[] {
                        new SqlParameter("jmeno", jmeno)
                        });

            if (res?.Count() > 0)
            {
                return res.Select(m => FromIco(m)).Where(m => m.Valid);
            }
            else
                return new Firma[] { };

        }

        public static IEnumerable<(string ico, string jmeno, bool isFop)> GetJmenoIcoAndTopTuples()
        {
            var data = DirectDB.Instance.GetList<string, string, bool>("select ico, jmeno, CASE WHEN Kod_PF <= 110 OR Kod_PF IS NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS isFOP from firma where ISNUMERIC(ico) = 1", CommandType.Text, null);
            return data.Select(m => (Util.ParseTools.NormalizeIco(m.Item1.Trim()), m.Item2.Trim(), m.Item3));

        }

        public static IEnumerable<string> GetEntrepreneurIcos()
        {
            var data = DirectDB.Instance.GetList<string>("select ico from firma where ISNUMERIC(ICO) = 1 AND Kod_PF <=110", CommandType.Text, null);
            return data;
        }

        public static IEnumerable<Firma> AllFromNameWildcards(string jmeno)
        {


            var sql = @"select ico from Firma where jmeno like @jmeno";

            var res = Connectors.DirectDB.Instance.GetList<string>(sql, param: new IDataParameter[] {
                        new SqlParameter("jmeno", jmeno)
                        });

            if (res?.Count() > 0)
            {
                return res.Select(m => FromIco(m)).Where(m => m.Valid);
            }
            else
                return new Firma[] { };

        }

        public static Firma FromDS(string ds)
        {
            string sql = @"select firma.ico from Firma_ds fds inner join firma on firma.ico = fds.ico where DatovaSchranka = @ds";

            var res = Connectors.DirectDB.Instance.GetList<string>(sql, param: new IDataParameter[] {
                        new SqlParameter("ds", ds)
                        });

            if (res?.Count() > 0)
            {
                return FromIco(res.First());
            }
            else
            {
                return Firma.NotFound;
            }


        }

        public static IEnumerable<string> AllIcoInRS()
        {
            string sql = @"select ico from Firma where IsInRS = 1";

            var res = DirectDB.Instance.GetList<string>(sql, CommandType.Text, null);

            if (res.Count() > 0)
            {
                var allIcos = res
                    .Where(r => TextUtil.IsNumeric(r))
                    .ToArray();

                return allIcos;
            }
            else
                return new string[] { };

        }

        //don;t use it, 
        /*        public static IEnumerable<Firma> AllFirmyInRS(bool skipDS_Nace = false)
                {
                    return AllIcoInRS()
                        .Select(m => FromIco(m))
                        .ToArray()
                        .Where(m => m.Valid);

                }
        */
        private static Firma FromDataRow(DataRow dr, bool skipDS_Nace = false)
        {
            Firma f = new Firma();
            f.ICO = (string)dr["ico"];
            f.DIC = (string)Devmasters.DirectSql.IsNull(dr["dic"], string.Empty);
            f.Datum_Zapisu_OR = (DateTime?)Devmasters.DirectSql.IsNull(dr["datum_zapisu_or"], null);
            f.Stav_subjektu = (byte)Convert.ToInt32(Devmasters.DirectSql.IsNull(dr["Stav_subjektu"], 1));
            f.Status = Convert.ToInt32(Devmasters.DirectSql.IsNull(dr["Status"], 1));
            f.Jmeno = (string)Devmasters.DirectSql.IsNull(dr["jmeno"], string.Empty);
            f.JmenoAscii = (string)Devmasters.DirectSql.IsNull(dr["jmenoascii"], string.Empty);
            f.Kod_PF = (int?)Devmasters.DirectSql.IsNull(dr["Kod_PF"], null);
            f.VersionUpdate = (int)dr["VersionUpdate"];
            f.IsInRS = (short?)Devmasters.DirectSql.IsNull(dr["IsInRS"], null);
            f.KrajId = (string)Devmasters.DirectSql.IsNull(dr["krajid"], string.Empty);
            f.OkresId = (string)Devmasters.DirectSql.IsNull(dr["okresid"], string.Empty);
            f.Typ = (int?)Devmasters.DirectSql.IsNull(dr["typ"], null);
            f.ESA2010 = (string)Devmasters.DirectSql.IsNull(dr["Esa2010"], string.Empty);

            if (skipDS_Nace == false)
            {

                f.DatovaSchranka = DirectDB.Instance.GetList<string>("select DatovaSchranka from firma_DS where ico=@ico", CommandType.Text, new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        })
                    .ToArray();

                f.NACE = DirectDB.Instance.GetList<string>("select NACE from firma_Nace where ico=@ico", CommandType.Text, new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        })
                    .ToArray();
            }

            return f;

        }

        public static string NameFromIco(string ico, bool IcoIfNotFound = false)
        {
            if (string.IsNullOrEmpty(ico))
                return string.Empty;


            string sql = @"select jmeno from Firma where ico = @ico";

            var res = DirectDB.Instance.GetValue<string>(sql, CommandType.Text, new IDataParameter[] {
                        new SqlParameter("ico", ico)
                        });

            if (Devmasters.DirectSql.IsNull(res) || string.IsNullOrEmpty(res as string))
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
        
    }


}