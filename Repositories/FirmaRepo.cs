using Devmasters;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Data.External.DatoveSchranky;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {

        private static async Task PrepareBeforeSaveAsync(Firma firma, bool updateLastUpdateValue = true)
        {
            firma.JmenoAscii = TextUtil.RemoveDiacritics(firma.Jmeno);
            await firma.SetTypAsync();

        }

        public static void Save(Firma firma)
        {
            //PrepareBeforeSaveAsync(firma).GetAwaiter().GetResult();

            if (firma.ICO == "00000205" ||  firma.ICO== "205" || firma.ICO == "_t20ABmHW5w")
            {
                HlidacStatu.Util.Consts.Logger.Fatal("00000205 Save\n\n" + Devmasters.Log.StackReporter.GetCallingMethod(true));
                HlidacStatu.Util.Consts.Logger.Debug("00000205 Save\n\n" + Devmasters.Log.StackReporter.GetCallingMethod(true));
            }

            string sqlNACE = @"INSERT into firma_NACE(ico, nace) values(@ico,@nace)";
            string sqlDS = @"INSERT into firma_DS(ico, DatovaSchranka) values(@ico,@DatovaSchranka)";

            try
            {
                using (DbEntities db = new DbEntities())
                {

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
                        HlidacStatu.Util.Consts.Logger.Error($"Saving EntityFr firma {firma.ICO}", e);
                    }


                    if (firma.DatovaSchranka != null)
                    {
                        HlidacStatu.Connectors.DirectDB.NoResult("delete from firma_DS where ico=@ico",
                            new IDataParameter[]
                            {
                                new SqlParameter("ico", firma.ICO)
                            });
                        foreach (var ds in firma.DatovaSchranka.Distinct())
                        {
                            HlidacStatu.Connectors.DirectDB.NoResult(sqlDS, new IDataParameter[]
                            {
                                new SqlParameter("ico", firma.ICO),
                                new SqlParameter("DatovaSchranka", ds),
                            });
                        }
                    }

                    if (firma.NACE != null)
                    {
                        HlidacStatu.Connectors.DirectDB.NoResult("delete from firma_NACE where ico=@ico",
                            new IDataParameter[]
                            {
                                new SqlParameter("ico", firma.ICO)
                            });
                        foreach (var nace in firma.NACE.Distinct())
                        {
                            HlidacStatu.Connectors.DirectDB.NoResult(sqlNACE, new IDataParameter[]
                            {
                                new SqlParameter("ico", firma.ICO),
                                new SqlParameter("nace", nace),
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error($"Saving firma {firma.ICO}", e);
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

        public static Firma FromName(string jmeno, bool getMissingFromExternal = false)
        {
            Firma f = FromName(jmeno);
            if (f != null)
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
            using (PersistLib p = new PersistLib())
            {
                string sql = @"insert into firma(ico,dic,stav_subjektu, jmeno, jmenoascii, versionupdate, popis)
                                values(@ico,@dic,@stav,@jmeno,@jmenoascii,0,@adresa)";

                p.ExecuteNonQuery(DirectDB.DefaultCnnStr, CommandType.Text, sql, new IDataParameter[] {
                    new SqlParameter("ico", ico),
                    new SqlParameter("dic", ico),
                    new SqlParameter("stav", (int)1),
                    new SqlParameter("jmeno", jmeno),
                    new SqlParameter("jmenoascii", TextUtil.RemoveDiacritics(jmeno)),
                    new SqlParameter("versionupdate", (long)0),
                    new SqlParameter("adresa", TextUtil.ShortenText(adresa,100)),
                    });
            }
        }

        public static Firma FromIco(string ico, bool loadAllData = true)
        {
            Firma f = new Firma();
            using (DbEntities db = new DbEntities())
            {

                f = db.Firma.FirstOrDefault(m => m.ICO == ico);
                if (f is null)
                    return Firma.NotFound;
                
                f.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(f.ICO);

                if (f != null)
                {
                    f.DatovaSchranka = Connectors.DirectDB.GetList<string>("select DatovaSchranka from firma_DS where ico=@ico", param: new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        })
                        .ToArray();

                    f.NACE = Connectors.DirectDB.GetList<string>("select NACE from firma_Nace where ico=@ico", param: new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        })
                        .ToArray();

                    return f;
                }
                else
                    return Firma.NotFound;

            }
        }

        public static Firma FromName(string jmeno)
        {
            var res = AllFromExactName(jmeno);
            return res.FirstOrDefault(Firma.NotFound);
            
        }

        public static IEnumerable<Firma> AllFromExactName(string jmeno)
        {
            string sql = @"select ico from Firma where jmeno = @jmeno";

            var res = Connectors.DirectDB.GetList<string>(sql, param: new IDataParameter[] {
                        new SqlParameter("jmeno", jmeno)
                        });

            if (res?.Count() > 0)
            {
                return res.Select(m => FromIco(m)).Where(m => m.Valid);
            }
            else
                return new Firma[] { };

        }

        public static IEnumerable<Firma> AllFromNameWildcards(string jmeno)
        {


            var sql = @"select ico from Firma where jmeno like @jmeno";

            var res = Connectors.DirectDB.GetList<string>(sql, param: new IDataParameter[] {
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

            var res = Connectors.DirectDB.GetList<string>(sql, param: new IDataParameter[] {
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
            using (PersistLib p = new PersistLib())
            {
                string sql = @"select ico from Firma where IsInRS = 1";

                var res = p.ExecuteDataset(DirectDB.DefaultCnnStr, CommandType.Text, sql, null);

                if (res.Tables.Count > 0 && res.Tables[0].Rows.Count > 0)
                {
                    var allIcos = res.Tables[0]
                        .AsEnumerable()
                        .Where(r => TextUtil.IsNumeric((string)r["ICO"]))
                        .Select(r => (string)r["ICO"])
                        .ToArray();

                    return allIcos;
                }
                else
                    return new string[] { };
            }
        }

        public static IEnumerable<Firma> AllFirmyInRS(bool skipDS_Nace = false)
        {
            return AllIcoInRS()
                .Select(m => FromIco(m))
                .Where(m => m.Valid);

        }

        private static Firma FromDataRow(DataRow dr, bool skipDS_Nace = false)
        {
            Firma f = new Firma();
            f.ICO = (string)dr["ico"];
            f.DIC = (string)PersistLib.IsNull(dr["dic"], string.Empty);
            f.Datum_Zapisu_OR = (DateTime?)PersistLib.IsNull(dr["datum_zapisu_or"], null);
            f.Stav_subjektu = (byte) Convert.ToInt32(PersistLib.IsNull(dr["Stav_subjektu"], 1));
            f.Status = Convert.ToInt32(PersistLib.IsNull(dr["Status"], 1));
            f.Jmeno = (string)PersistLib.IsNull(dr["jmeno"], string.Empty);
            f.JmenoAscii = (string)PersistLib.IsNull(dr["jmenoascii"], string.Empty);
            f.Kod_PF = (int?)PersistLib.IsNull(dr["Kod_PF"], null);
            f.VersionUpdate = (int)dr["VersionUpdate"];
            f.IsInRS = (short?)PersistLib.IsNull(dr["IsInRS"], null);
            f.KrajId = (string)PersistLib.IsNull(dr["krajid"], string.Empty);
            f.OkresId = (string)PersistLib.IsNull(dr["okresid"], string.Empty);
            f.Typ = (int?)PersistLib.IsNull(dr["typ"], null);
            f.ESA2010 = (string)PersistLib.IsNull(dr["Esa2010"], string.Empty);

            if (skipDS_Nace == false)
            {
                using (PersistLib p = new PersistLib())
                {
                    f.DatovaSchranka = p.ExecuteDataset(DirectDB.DefaultCnnStr, CommandType.Text, "select DatovaSchranka from firma_DS where ico=@ico", new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        }).Tables[0]
                        .AsEnumerable()
                        .Select(m => m[0].ToString())
                        .ToArray();

                    f.NACE = p.ExecuteDataset(DirectDB.DefaultCnnStr, CommandType.Text, "select NACE from firma_Nace where ico=@ico", new IDataParameter[] {
                        new SqlParameter("ico", f.ICO)
                        }).Tables[0]
                        .AsEnumerable()
                        .Select(m => m[0].ToString())
                        .ToArray();
                }
            }
            return f;

        }

        public static string NameFromIco(string ico, bool IcoIfNotFound = false)
        {
            using (PersistLib p = new PersistLib())
            {
                string sql = @"select jmeno from Firma where ico = @ico";

                var res = p.ExecuteScalar(DirectDB.DefaultCnnStr, CommandType.Text, sql, new IDataParameter[] {
                        new SqlParameter("ico", ico)
                        });

                if (PersistLib.IsNull(res) || string.IsNullOrEmpty(res as string))
                {
                    if (IcoIfNotFound)
                        return "IČO:" + ico;
                    else
                        return string.Empty;
                }
                else
                    return (string)res;

            }
        }

        public static void RefreshDS(this Firma firma)
        {
            firma.DatovaSchranka = Lib.Data.External.DatoveSchranky.ISDS.GetDatoveSchrankyForIco(firma.ICO);
        }

    }
}