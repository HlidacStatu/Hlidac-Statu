using Devmasters;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Data.External.DatoveSchrankyOpenData;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Nest;
using Devmasters.Enums;

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

        private static async Task PrepareBeforeSaveAsync(Firma firma, bool updateLastUpdateValue = true)
        {
            firma.JmenoAscii = TextUtil.RemoveDiacritics(firma.Jmeno);
            await firma.SetTypAsync();

        }

        public static void Save(Firma firma)
        {
            //PrepareBeforeSaveAsync(firma).GetAwaiter().GetResult();


            string sqlNACE = @"INSERT into firma_NACE(ico, nace) values(@ico,@nace)";
            //string sqlDS = @"INSERT into firma_DS(ico, DatovaSchranka) values(@ico,@DatovaSchranka)";

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
                        _logger.Error(e, $"Saving EntityFr firma {firma.ICO}");
                    }


                    /*                    if (firma.DatovaSchranka != null)
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
                                        }*/

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

        public static IEnumerable<(string ico, string jmeno, bool isFop)> GetJmenoIcoAndFopTuples()
        {
            using (PersistLib p = new PersistLib())
            {
                var reader = p.ExecuteReader(DirectDB.DefaultCnnStr, CommandType.Text, "select ico, jmeno, CASE WHEN Kod_PF <= 110 OR Kod_PF IS NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS isFOP from firma where ISNUMERIC(ico) = 1", null);
                while (reader.Read())
                {
                    string ico = reader.GetString(0).Trim();
                    string name = reader.GetString(1).Trim();
                    bool isFop = reader.GetBoolean(2);
                    if (Devmasters.TextUtil.IsNumeric(ico))
                    {
                        ico = Util.ParseTools.NormalizeIco(ico);
                        yield return (ico, name, isFop);
                    }
                }
            }
        }

        public static IEnumerable<string> GetEntrepreneurIcos()
        {
            using (PersistLib p = new PersistLib())
            {
                var reader = p.ExecuteReader(DirectDB.DefaultCnnStr, CommandType.Text, "select ico from firma where ISNUMERIC(ICO) = 1 AND Kod_PF <=110", null);
                while (reader.Read())
                {
                    yield return reader.GetString(0).Trim();
                }
            }
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
            f.DIC = (string)PersistLib.IsNull(dr["dic"], string.Empty);
            f.Datum_Zapisu_OR = (DateTime?)PersistLib.IsNull(dr["datum_zapisu_or"], null);
            f.Stav_subjektu = (byte)Convert.ToInt32(PersistLib.IsNull(dr["Stav_subjektu"], 1));
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
            if (string.IsNullOrEmpty(ico))
                return string.Empty;


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
            firma.DatovaSchranka = Lib.Data.External.DatoveSchrankyOpenData.ISDS.GetDatoveSchrankyForIco(firma.ICO);
        }
        public static HlidacStatu.DS.Api.Firmy.SubjektFinancialInfo GetFinancialInfo(string ico, string name)
        {
            DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny;
                Firma f = NajdiFirmuPodleIcoJmena(ico, name, aktualnost);
            if (f == null || f.Valid == false)
            {
                return null;
            }
            HlidacStatu.DS.Api.Firmy.SubjektFinancialInfo res = new ();
            res.Source_Url = f.GetUrl(false);
            res.Ico = f.ICO;
            res.Jmeno_Firmy = f.Jmeno;
            res.Omezeni_Cinnosti = string.IsNullOrWhiteSpace( f.StatusFull()) ? null : f.StatusFull() ;

            res.Kategorie_Organu_Verejne_Moci = null;
            var _kategorie_Organu_Verejne_Moci = f.KategorieOVMAsync().ConfigureAwait(false).GetAwaiter().GetResult()
                .Select(m => m.nazev)
                .ToArray();
            if (_kategorie_Organu_Verejne_Moci?.Length > 0)
                res.Kategorie_Organu_Verejne_Moci = _kategorie_Organu_Verejne_Moci;

            res.Pocet_Zamestnancu = FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyMagnitude(f.PocetZamKod?.ToString())?.Pretty;
            res.Obrat = FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyTurnover(f.ObratKod?.ToString())?.Pretty;

            res.Obor_Podnikani = FirmaRepo.Merk.MerkEnumConverters.ConvertCompanyIndustryToFullName(f.IndustryKod?.ToString(),true,true);
            res.Platce_DPH = f.PlatceDPHKod switch
            {
                1 => "Je plátce DPH",
                0 => "Není plátce DPH",
                _ => null
            };

            res.Je_nespolehlivym_platcem_DPHKod = f.Je_nespolehlivym_platcem_DPHKod switch
            {
                1 => "Je nespolehlivým plátcem DPH",
                _ => null
            };

            res.Ma_dluh_vzp = f.Ma_dluh_vzpKod switch
            {
                1 => "Má dluh vůči VZP",
                _ => null
            };
            res.Osoby_s_vazbou_na_firmu = f.Osoby_v_OR(aktualnost)
                .OrderBy(m => m.o.Prijmeni)
                    .ThenBy(m => m.o.Jmeno)
                    .ThenBy(m => m.o.Narozeni)
                    .Select(m => new HlidacStatu.DS.Api.Firmy.SubjektFinancialInfo.Osoba()
                    {
                        Full_Name = m.o.FullName(),
                        Person_Id = m.o.NameId,
                        Year_Of_Birth = m.o.Narozeni.HasValue ? m.o.Narozeni.Value.Year.ToString() : null,
                    })
                    .ToArray()
                ;

            return res; 
        }
        public static HlidacStatu.DS.Api.Firmy.SubjektDetailInfo GetDetailInfo(string ico, string name)
        {
            DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny;

            Firma f = NajdiFirmuPodleIcoJmena(ico, name, aktualnost);

            if (f == null || f.Valid == false)
            {
                return null;
            }

            // do work
            HlidacStatu.DS.Api.Firmy.SubjektDetailInfo res = new();

            res.Business_info = GetFinancialInfo(f.ICO, f.Jmeno);
            if (res.Business_info != null)
            {
                //remove here to avoid duplicated data
                res.Business_info.Source_Url = null;
                res.Business_info.Copyright = null;
                res.Business_info.Jmeno_Firmy = null;
            }

            res.Source_Url = f.GetUrl(false);
            res.Ico = f.ICO;
            res.Jmeno_Firmy = f.Jmeno;
            res.Rizika = f.InfoFacts().RenderFacts(4, true, false);
            
            res.Osoby_s_vazbou_na_firmu = f.Osoby_v_OR(aktualnost)
                .OrderBy(m => m.o.Prijmeni)
                    .ThenBy(m => m.o.Jmeno)
                    .ThenBy(m => m.o.Narozeni)
                    .Select(m => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.Osoba()
                    {
                        Full_Name = m.o.FullName(),
                        Person_Id = m.o.NameId,
                        Year_Of_Birth = m.o.Narozeni.HasValue ? m.o.Narozeni.Value.Year.ToString() : null,
                    })
                    .ToArray()
                ;


            res.Kategorie_Organu_Verejne_Moci = f.KategorieOVMAsync().ConfigureAwait(false).GetAwaiter().GetResult()
                .Select(m => m.nazev)
                .ToArray();

            if (f.JsemNeziskovka())
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.NeziskovaOrganizace;
            else if (f.JsemOVM())
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.StatniOrgan;
            else if (f.JsemPolitickaStrana())
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.PolitickaStrana;
            else if (f.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatu
                || f.TypSubjektu == Firma.TypSubjektuEnum.PatrimStatuAlespon25perc
                || f.JsemStatniFirma())
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.FirmaPatriStatu;
            else if (f.Registrovana_v_zahranici)
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.ZahranicniFirma;
            else if (f.JsemSoukromaFirma())
                res.Charakter_Firmy = DS.Api.Firmy.SubjektDetailInfo.CharakterEnum.SoukromaFirma;



            //KINDEX
            Entities.KIndex.KIndexData kindex = f.KindexAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (kindex != null)
            {
                var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearKIndex;
                for (int i = 0; i <= 3; i++)
                {
                    var kidx = kindex.ForYear(maxY - i);
                    if (kidx != null)
                    {
                        res.KIndex.Add(new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.KIndexData()
                        {
                            KIndex = kidx.KIndexLabel.ToString(),
                            Obrazek_Url = kidx.KIndexLabelIconUrl(false),
                            Popis = kindex.InfoFacts(i).RenderFacts(3, true, true, ", "),
                            Rok = i
                        });
                    }
                }
            }

            //Smlouvy
            var smlouvyStat = f.StatistikaRegistruSmluv();
            if (smlouvyStat != null)
            {
                var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearSmlouvy;
                res.Statistika_Registr_Smluv = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                {
                    Rok = 0,
                    CelkovaHodnotaSmluv = smlouvyStat.Summary().CelkovaHodnotaSmluv,
                    HlavniOblasti = smlouvyStat.Summary().PoOblastech
                                .OrderByDescending(o => o.Value.CelkemCena)
                                .ThenByDescending(o => o.Value.Pocet)
                                .Take(3)
                                .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                                .ToArray(),
                    PocetSmluv = smlouvyStat.Summary().PocetSmluv,
                    PocetSmluvBezCeny = smlouvyStat.Summary().PocetSmluvBezCeny,
                    PocetSmluvSeZasadnimNedostatkem = smlouvyStat.Summary().PocetSmluvSeZasadnimNedostatkem,
                    PocetSmluvULimitu = smlouvyStat.Summary().PocetSmluvULimitu
                };

                res.Statistiky_Registr_Smluv_po_Letech = smlouvyStat
                    .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                    .Select(ss => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                    {
                        Rok = ss.Year,
                        CelkovaHodnotaSmluv = ss.Value.CelkovaHodnotaSmluv,
                        HlavniOblasti = ss.Value.PoOblastech
                                .OrderByDescending(o => o.Value.CelkemCena)
                                .ThenByDescending(o => o.Value.Pocet)
                                .Take(3)
                                .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                                .ToArray(),
                        PocetSmluv = ss.Value.PocetSmluv,
                        PocetSmluvBezCeny = ss.Value.PocetSmluvBezCeny,
                        PocetSmluvSeZasadnimNedostatkem = ss.Value.PocetSmluvSeZasadnimNedostatkem,
                        PocetSmluvULimitu = ss.Value.PocetSmluvULimitu,
                        ZmenaHodnotySmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Hodnota smluv", ss.Value.CelkovaHodnotaSmluv, smlouvyStat.StatisticsForYear(maxY).CelkovaHodnotaSmluv),
                        ZmenaPoctuSmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Počet smluv", ss.Value.PocetSmluv, smlouvyStat.StatisticsForYear(maxY).PocetSmluv),
                    })
                    .ToList();
            }


            //dotace
            var dotaceStat = f.StatistikaDotaci();
            if (dotaceStat != null)
            {
                var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearDotace;
                res.Statistika_Dotace = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                {
                    Rok = 0,
                    Celkem_Prideleno = dotaceStat.Summary().CelkemPrideleno,
                    Pocet_Dotaci = dotaceStat.Summary().PocetDotaci
                };

                res.Statistika_Dotace_po_Letech = dotaceStat
                    .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                    .Select(ds => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                    {
                        Rok = ds.Year,
                        Celkem_Prideleno = ds.Value.CelkemPrideleno,
                        Pocet_Dotaci = ds.Value.PocetDotaci,
                        //ZmenaHodnotyDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Hodnota smluv", ds.Value.CelkemPrideleno, dotaceStat.StatisticsForYear(maxY).CelkemPrideleno),
                        //ZmenaPoctuDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Počet smluv", ds.Value.PocetDotaci, dotaceStat.StatisticsForYear(maxY).PocetDotaci),
                    })
                    .ToList();
            }
            if (f.Holding(aktualnost)?.Any() == true)
            {
                var smlouvyStatHolding = f.HoldingStatisticsRegistrSmluv(aktualnost);
                var maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearSmlouvy;
                if (smlouvyStatHolding != null)
                {
                    res.Statistika_Registr_Smluv_pro_Holding = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                    {
                        Rok = 0,
                        CelkovaHodnotaSmluv = smlouvyStatHolding.Summary().CelkovaHodnotaSmluv,
                        HlavniOblasti = smlouvyStatHolding.Summary().PoOblastech
                            .OrderByDescending(o => o.Value.CelkemCena)
                            .ThenByDescending(o => o.Value.Pocet)
                            .Take(3)
                            .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                            .ToArray(),
                        PocetSmluv = smlouvyStatHolding.Summary().PocetSmluv,
                        PocetSmluvBezCeny = smlouvyStatHolding.Summary().PocetSmluvBezCeny,
                        PocetSmluvSeZasadnimNedostatkem = smlouvyStatHolding.Summary().PocetSmluvSeZasadnimNedostatkem,
                        PocetSmluvULimitu = smlouvyStatHolding.Summary().PocetSmluvULimitu
                    };

                    res.Statisticky_Registr_Smluv_pro_Holding_po_Letech = smlouvyStatHolding
                        .Where(m => m.Year <= maxY && m.Year >= maxY - 3)
                        .Select(ss => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.SmlouvyData()
                        {
                            Rok = ss.Year,
                            CelkovaHodnotaSmluv = ss.Value.CelkovaHodnotaSmluv,
                            HlavniOblasti = ss.Value.PoOblastech
                                    .OrderByDescending(o => o.Value.CelkemCena)
                                    .ThenByDescending(o => o.Value.Pocet)
                                    .Take(3)
                                    .Select(m => ((Smlouva.SClassification.ClassificationsTypes?)m.Key).ToNiceDisplayName())
                                    .ToArray(),
                            PocetSmluv = ss.Value.PocetSmluv,
                            PocetSmluvBezCeny = ss.Value.PocetSmluvBezCeny,
                            PocetSmluvSeZasadnimNedostatkem = ss.Value.PocetSmluvSeZasadnimNedostatkem,
                            PocetSmluvULimitu = ss.Value.PocetSmluvULimitu,
                            ZmenaHodnotySmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Hodnota smluv", ss.Value.CelkovaHodnotaSmluv, smlouvyStatHolding.StatisticsForYear(maxY).CelkovaHodnotaSmluv),
                            ZmenaPoctuSmluv = ss.Year == maxY ? null : new DS.Api.StatisticChange(ss.Year, maxY, "Počet smluv", ss.Value.PocetSmluv, smlouvyStatHolding.StatisticsForYear(maxY).PocetSmluv),
                        })
                        .ToList();
                }

                var dotaceStatHolding = f.HoldingStatistikaDotaci(aktualnost);
                maxY = HlidacStatu.Util.Consts.CalculatedCurrentYearDotace;
                if (dotaceStatHolding != null)
                {
                    res.Statistika_Dotace_pro_Holding = new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                    {
                        Rok = 0,
                        Celkem_Prideleno = dotaceStatHolding.Summary().CelkemPrideleno,
                        Pocet_Dotaci = dotaceStatHolding.Summary().PocetDotaci
                    };

                    res.Statistika_Dotace_pro_Holding_po_Letech = dotaceStatHolding
                        .Where(m => m.Year <= HlidacStatu.Util.Consts.CalculatedCurrentYearDotace && m.Year >= HlidacStatu.Util.Consts.CalculatedCurrentYearDotace - 3)
                        .Select(ds => new HlidacStatu.DS.Api.Firmy.SubjektDetailInfo.DotaceData()
                        {
                            Rok = ds.Year,
                            Celkem_Prideleno = ds.Value.CelkemPrideleno,
                            Pocet_Dotaci = ds.Value.PocetDotaci,
                            //ZmenaHodnotyDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Hodnota smluv", ds.Value.CelkemPrideleno, dotaceStatHolding.StatisticsForYear(maxY).CelkemPrideleno),
                            //ZmenaPoctuDotaci = ds.Year == maxY ? null : new DS.Api.StatisticChange(ds.Year, maxY, "Počet smluv", ds.Value.PocetDotaci, dotaceStatHolding.StatisticsForYear(maxY).PocetDotaci),
                        })
                        .ToList();
                }
            } //if has holding

            return res;
        }

        private static Firma NajdiFirmuPodleIcoJmena(string ico, string name, DS.Graphs.Relation.AktualnostType aktualnost = DS.Graphs.Relation.AktualnostType.Nedavny)
        {
            Firma f = null;
            if (HlidacStatu.Util.DataValidators.CheckCZICO(ico) == false)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var fname = Firma.JmenoBezKoncovky(name);
                    var found = FirmaRepo.Searching.FindAllAsync(name, 5, true).ConfigureAwait(false).GetAwaiter().GetResult();

                    List<(Firma f, int diffs)> diff = found
                        .Select(m => (m, HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(name, m.Jmeno.Trim())))
                        .ToList();
                    if (diff.Any(m => m.diffs == 0) == false)
                        diff = found
                            .Select(m => (m, HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(fname, m.JmenoBezKoncovky())))
                            .ToList();
                    if (diff.Any(m => m.diffs == 0) == false)
                        diff = found
                            .Select(m => (m, HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(fname.Trim().ToLower(), m.JmenoBezKoncovky().Trim().ToLower())))
                            .ToList();

                    if (diff.Any(m => m.diffs == 0))
                        f = diff.First(m => m.diffs == 0).f;
                    else if (diff.Any(m => m.diffs <= 1))
                    {
                        f = diff.First(m => m.diffs <= 1).f;
                    }
                }
            }
            else
            {
                f = Firmy.Get(ico);
            }

            return f;
        }
    }


}