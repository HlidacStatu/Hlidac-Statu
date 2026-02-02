using Devmasters;
using Devmasters.Enums;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;


namespace HlidacStatu.Repositories
{
    public static partial class Graph
    {
        static DateTime minDate = new DateTime(1950, 1, 1);

        public static IEnumerable<string> UniqIds(IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> edges)
        {
            if (edges == null)
                return new string[] { };
            var uids = edges
                .Select(m => m.From?.UniqId)
                .Where(m => m != null)
                .Union(edges
                    .Select(m => m.To?.UniqId)
                    .Where(m => m != null)
                ).Distinct();

            return uids;
        }

        private static IFusionCache MemcachedCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(Graph));

        const string vazbyCacheVersion = "V2";

        private static string GetVsechnyDcerineVazbyOsobaKey(string nameId,
            Relation.CharakterVazbyEnum charakterVazbyEnum) =>
            $"_VsechnyDcerineVazbyOsoba_{vazbyCacheVersion}:{nameId}_{charakterVazbyEnum}";
        
        private static ValueTask<List<HlidacStatu.DS.Graphs.Graph.Edge>> GetVazbyOsobaNameIdAsync(string nameId,
            Relation.CharakterVazbyEnum charakterVazbyEnum) =>
            MemcachedCache.GetOrSetAsync(GetVsechnyDcerineVazbyOsobaKey(nameId, charakterVazbyEnum),
                async _ =>
                {
                    if (string.IsNullOrEmpty(nameId))
                        return new List<HlidacStatu.DS.Graphs.Graph.Edge>();
                    Osoba o = await OsobaCache.GetPersonByNameIdAsync(nameId);
                    return await VsechnyDcerineVazbyInternalAsync(o, charakterVazbyEnum, 0, true, null);
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(3)));


        private static string GetVsechnyDcerineVazbyIcoKey(string ico,
            Relation.CharakterVazbyEnum charakterVazbyEnum) =>
            $"_VsechnyDcerineVazbyIco_{vazbyCacheVersion}:{ico}_{charakterVazbyEnum}";
        
        private static Task<List<HlidacStatu.DS.Graphs.Graph.Edge>> GetVazbyIcoAsync(string ico,
            Relation.CharakterVazbyEnum charakterVazbyEnum) =>
            MemcachedCache.GetOrSetAsync(GetVsechnyDcerineVazbyIcoKey(ico, charakterVazbyEnum),
                async _ => await VsechnyDcerineVazbyInternalAsync(ico, charakterVazbyEnum, 0, true, null),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(3))).AsTask();
        

        public static async Task SmazVsechnyDcerineVazbyFirmyAsync(string ico)
        {
            foreach (Relation.CharakterVazbyEnum charakter in Enum.GetValues(typeof(Relation.CharakterVazbyEnum)))
                await MemcachedCache.RemoveAsync(GetVsechnyDcerineVazbyIcoKey(ico, charakter));
        }

        public static async Task<List<DS.Graphs.Graph.Edge>> VsechnyDcerineVazbyAsync(string ico,
            Relation.CharakterVazbyEnum charakterVazby,
            bool refresh = false)
        {
            if (refresh)
                await MemcachedCache.RemoveAsync(GetVsechnyDcerineVazbyIcoKey(ico, charakterVazby));
            return await GetVazbyIcoAsync(ico, charakterVazby);
        }

        public static async Task SmazVsechnyDcerineVazbyOsobyAsync(long internalId)
        {
            var nameId = await DirectDB.Instance.GetValueAsync<string>("select nameid from Osoba where internalId=@internalId",
                param: new SqlParameter[] { new SqlParameter("internalId", internalId) });
            if (!string.IsNullOrEmpty(nameId))
            {
                foreach (Relation.CharakterVazbyEnum charakter in Enum.GetValues(typeof(Relation.CharakterVazbyEnum)))
                    await MemcachedCache.RemoveAsync(GetVsechnyDcerineVazbyOsobaKey(nameId, charakter));
            }
        }

        public static async Task SmazVsechnyDcerineVazbyOsobyAsync(string nameId)
        {
            foreach (Relation.CharakterVazbyEnum charakter in Enum.GetValues(typeof(Relation.CharakterVazbyEnum)))
                await MemcachedCache.RemoveAsync(GetVsechnyDcerineVazbyOsobaKey(nameId, charakter));
        }


        public static async Task<List<DS.Graphs.Graph.Edge>> VsechnyDcerineVazbyAsync(Osoba person,
            Relation.CharakterVazbyEnum charakterVazby,
            bool refresh = false)
        {
            if (person == null)
                return null;
            if (refresh)
                await MemcachedCache.RemoveAsync(GetVsechnyDcerineVazbyOsobaKey(person.NameId, charakterVazby));
            return await GetVazbyOsobaNameIdAsync(person.NameId, charakterVazby);
        }

        private static async Task<List<DS.Graphs.Graph.Edge>> VsechnyDcerineVazbyInternalAsync(
            string ico,
            Relation.CharakterVazbyEnum charakterVazby,
            int level, bool goDeep, HlidacStatu.DS.Graphs.Graph.Edge parent,
            ExcludeDataCol excludeICO = null, DateTime? datumOd = null, DateTime? datumDo = null, decimal minPodil = 0)
        {
            var charakterVazbyQuery = "";
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
            {
                charakterVazbyQuery =
                    $"and typVazby not in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }
            else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
            {
                charakterVazbyQuery = $"and typVazby in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }

            string sql = $@"select vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from Firmavazby 
    where ico=@ico 
    {charakterVazbyQuery}
    and (podil is null or podil >= {minPodil})
	and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
";
            var p = new SqlParameter[]
            {
                new SqlParameter("ico", ico),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            };

            var rel = await GetChildrenRelationsAsync(sql, charakterVazby,
                HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company, ico, datumOd, datumDo,
                p, level, goDeep, parent, excludeICO, minPodil);
            return rel;
        }


        private static async Task<List<DS.Graphs.Graph.Edge>> VsechnyDcerineVazbyInternalAsync(Osoba person,
            Relation.CharakterVazbyEnum charakterVazby,
            int level, bool goDeep, HlidacStatu.DS.Graphs.Graph.Edge parent,
            ExcludeDataCol excludeICO = null, IEnumerable<int> excludeOsobaId = null,
            DateTime? datumOd = null, DateTime? datumDo = null, decimal minPodil = 0)
        {
            if (excludeOsobaId == null)
                excludeOsobaId = new int[] { };

            var charakterVazbyQuery = "";
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
            {
                charakterVazbyQuery =
                    $"and typVazby not in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }
            else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
            {
                charakterVazbyQuery = $"and typVazby in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }

            string sql =
                $@"select vazbakIco, vazbakOsobaId, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from OsobaVazby 
                            where osobaId = @osobaId 
                                {charakterVazbyQuery}
                            and (podil is null or podil >= {minPodil})
                	        and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
";
            var p = new SqlParameter[]
            {
                new SqlParameter("osobaId", person.InternalId),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            };

            var relForPerson = await GetChildrenRelationsAsync(sql, charakterVazby,
                HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person, person.InternalId.ToString(),
                datumOd, datumDo,
                p, level, goDeep, parent, excludeICO, minPodil);

            var relForConnectedPersons = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            await using (DbEntities db = new DbEntities())
            {
                var navazaneOsoby = (await db.OsobaVazby.AsNoTracking()
                    .Where(m => m.OsobaId == person.InternalId
                                && m.VazbakOsobaId != null
                    )
                    .ToArrayAsync()) //get data from DB
                    //filter by date
                    .Where(o => Devmasters.DT.Util.IsOverlappingIntervals(datumOd, datumDo, o.DatumOd, o.DatumDo) ==
                                true)
                    .ToList();


                if (navazaneOsoby.Count > 0)
                    foreach (var ov in navazaneOsoby)
                    {
                        HlidacStatu.DS.Graphs.Graph.Edge parentRelFound = relForPerson
                            .Where(r => r.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person
                                        && r.To.Id == ov.VazbakOsobaId.Value.ToString())
                            .FirstOrDefault();

                        if (!excludeOsobaId.Contains(ov.VazbakOsobaId.Value))
                        {
                            Osoba o = await OsobaCache.GetPersonByIdAsync(ov.VazbakOsobaId.Value);
                            excludeOsobaId =
                                excludeOsobaId.Union(new int[]
                                {
                                    ov.VazbakOsobaId.Value, ov.OsobaId
                                }); //pridej obe osoby pro zamezeni kruhu pri vzajemnem provazani
                            var rel = await VsechnyDcerineVazbyInternalAsync(o, charakterVazby, level + 1, true, parentRelFound,
                                excludeOsobaId: excludeOsobaId);
                            relForConnectedPersons =
                                HlidacStatu.DS.Graphs.Graph.Edge.Merge(relForConnectedPersons, rel);
                        }
                    }
            }

            var finalRel = HlidacStatu.DS.Graphs.Graph.Edge.Merge(relForConnectedPersons, relForPerson);
            return finalRel;
        }

        public static async Task<List<DS.Graphs.Graph.Edge>> HoldingAsync(string ico, Relation.AktualnostType aktualnost)
        {
            DateTime? from = null;
            DateTime to = DateTime.Now.Date.AddDays(1);
            switch (aktualnost)
            {
                case Relation.AktualnostType.Aktualni:
                    from = to.AddDays(-1);
                    break;
                case Relation.AktualnostType.Nedavny:
                    from = to - Relation.NedavnyVztahDelka;
                    break;
                case Relation.AktualnostType.Neaktualni:
                case Relation.AktualnostType.Libovolny:
                    from = new DateTime(1950, 1, 1);
                    break;
                default:
                    break;
            }

            return await HoldingAsync(ico, from.Value, to, aktualnost);
        }

        public static async Task<List<DS.Graphs.Graph.Edge>> HoldingAsync(string ico, DateTime datumOd, DateTime datumDo,
            Relation.AktualnostType aktualnost)
        {
            var vazby = await VsechnyDcerineVazbyInternalAsync(ico, Relation.CharakterVazbyEnum.VlastnictviKontrola, 9, true, null,
                datumOd: datumOd, datumDo: datumDo);
            var parents = await GetParentRelationsAsync(ico, Relation.CharakterVazbyEnum.VlastnictviKontrola, vazby, 0, datumOd,
                datumDo);
            var rootNode = new HlidacStatu.DS.Graphs.Graph.Node()
                { Id = ico, Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company };
            if (parents?.Count() > 0)
            {
                if (vazby.Any(m => m.Root))
                    if (vazby.Any(m => m.From == null && m.To.UniqId == rootNode.UniqId))
                    {
                        vazby = vazby
                            .Where(m => !(m.From == null && m.To.UniqId != rootNode.UniqId))
                            .ToList();
                    }
            }

            var finalRel = HlidacStatu.DS.Graphs.Graph.Edge.Merge(parents.ToList(), vazby);
            return finalRel;
        }

        public static async Task<List<DS.Graphs.Graph.Edge>> GetChildrenRelationsAsync(string sql,
            Relation.CharakterVazbyEnum charakterVazby,
            HlidacStatu.DS.Graphs.Graph.Node.NodeType nodeType, string nodeId, DateTime? datumOd, DateTime? datumDo,
            SqlParameter[] parameters, int level, bool goDeep,
            HlidacStatu.DS.Graphs.Graph.Edge parent, ExcludeDataCol excludeICO, decimal minPodil)
        {
            if (excludeICO == null)
                excludeICO = new ExcludeDataCol();

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<HlidacStatu.DS.Graphs.Graph.Edge> relations = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            if (level == 0 && parent == null)
            {
                //add root node / edge
                relations.Add(
                    new HlidacStatu.DS.Graphs.Graph.Edge()
                    {
                        From = null,
                        Root = true,
                        To = new HlidacStatu.DS.Graphs.Graph.Node() { Id = nodeId, Type = nodeType },
                        RelFrom = datumOd,
                        RelTo = datumDo,
                        Distance = 0
                    }
                );
            }
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new Devmasters.DbConnect();
#pragma warning 
            var sqlCall = DirectDB.Instance.GetRawSql(sql, parameters);
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = await db.ExecuteDatasetAsync(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                List<AngazovanostData> rows = new List<AngazovanostData>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    AngazovanostData angaz = null;
                    var ico = (string)dr["VazbakIco"];
                    if (string.IsNullOrEmpty(ico))
                    {
                        if (dr.Table.Columns.Contains("vazbakOsobaId"))
                        {
                            var vazbakOsobaId = (int?)DirectSql.IsNull(dr["vazbakOsobaId"], null);
                            if (vazbakOsobaId != null)
                            {
                                Osoba o = await OsobaCache.GetPersonByIdAsync(vazbakOsobaId.Value);
                                angaz = new AngazovanostData()
                                {
                                    subjId = vazbakOsobaId.Value.ToString(),
                                    NodeType = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person,
                                    fromDate = (DateTime?)DirectSql.IsNull(dr["datumOd"], null),
                                    toDate = (DateTime?)DirectSql.IsNull(dr["datumDo"], null),
                                    kod_ang = Convert.ToInt32(dr["typVazby"]),
                                    descr = (string)DirectSql.IsNull(dr["PojmenovaniVazby"], ""),
                                    podil = (decimal?)DirectSql.IsNull(dr["podil"], null)
                                };
                            }
                        }
                    }
                    else
                    {
                        angaz = new AngazovanostData()
                        {
                            subjId = ico,
                            subjname = "",
                            NodeType = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company,
                            fromDate = (DateTime?)DirectSql.IsNull(dr["datumOd"], null),
                            toDate = (DateTime?)DirectSql.IsNull(dr["datumDo"], null),
                            kod_ang = Convert.ToInt32(dr["typVazby"]),
                            descr = (string)DirectSql.IsNull(dr["PojmenovaniVazby"], ""),
                            podil = (decimal?)DirectSql.IsNull(dr["podil"], null)
                        };
                    }

                    rows.Add(angaz);
                }

                List<AngazovanostData> filteredRels = new List<AngazovanostData>();
                //delete vazby ve stejnem obdobi
                if (rows.Count > 0)
                {
                    //per ico
                    foreach (var gIco in rows.Select(m => m.subjId).Distinct())
                    {
                        var relsForIco = rows.Where(m => m.subjId == gIco);
                        //find longest, or separate relation
                        foreach (var r in relsForIco)
                        {
                            if (relsForIco
                                .Any(rr =>
                                    rr != r
                                    && rr.fromDate <= r.fromDate
                                    && (rr.toDate >= r.toDate || rr.toDate.HasValue == false)
                                )
                               )
                            {
                                //skip
                            }
                            else
                                filteredRels.Add(r);
                        }
                    }
                }
                
                foreach (AngazovanostData ang in filteredRels.OrderBy(m => m.kod_ang))
                {
                    if (
                        ang.kod_ang ==
                        100) //souhrny (casove) vztah, zkontroluj, zda uz tam neni jiny vztah se stejnym rozsahem doby
                    {
                        if (
                            relations.Any(r => r.To.Id == ang.subjId
                                               && r.To.Type == ang.NodeType
                                               && r.RelFrom == ang.fromDate
                                               && r.RelTo == ang.toDate
                            )
                        )
                            continue;
                    }

                    var rel = AngazovanostDataToEdge(ang,
                        new HlidacStatu.DS.Graphs.Graph.Node() { Type = nodeType, Id = nodeId },
                        new HlidacStatu.DS.Graphs.Graph.Node() { Type = ang.NodeType, Id = ang.subjId },
                        level + 1
                    );


                    if (excludeICO.Contains(rel))
                        continue; //skip to the next

                    relations.Add(rel);
                }
            }


            List<DS.Graphs.Graph.Edge> relationsWithLongestEdges =
                HlidacStatu.DS.Graphs.Graph.Edge.GetLongestEdges(relations).ToList();

            if (goDeep && relationsWithLongestEdges.Count > 0)
            {
                level++;
                List<HlidacStatu.DS.Graphs.Graph.Edge> deeperRels = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
                
                foreach (var rel in relationsWithLongestEdges.Where(m => m.Root == false))
                {
                    //old
                    deeperRels.AddRange(
                        await VsechnyDcerineVazbyInternalAsync(rel.To.Id, charakterVazby, level, goDeep, rel,
                            excludeICO.AddItem(new ExcludeData(rel)),
                            rel.RelFrom, rel.RelTo, minPodil)
                    );
                }

                relationsWithLongestEdges.AddRange(deeperRels);
            }

            if (level == 0)
            {
                //remove inactive companies from last branches
                //TODO
            }

            return relationsWithLongestEdges;
        }

        private static HlidacStatu.DS.Graphs.Graph.Edge AngazovanostDataToEdge(AngazovanostData ang,
            HlidacStatu.DS.Graphs.Graph.Node fromNode, HlidacStatu.DS.Graphs.Graph.Node toNode, int distance)
        {
            var rel = new HlidacStatu.DS.Graphs.Graph.Edge();
            rel.From = fromNode;
            rel.To = toNode;
            rel.Distance = distance;
            rel.VazbaType = ang.kod_ang;
            rel.RelFrom = (DateTime?)DirectSql.IsNull(ang.fromDate, null);
            if (rel.RelFrom < minDate)
                rel.RelFrom = null;

            rel.RelTo = (DateTime?)DirectSql.IsNull(ang.toDate, null);
            if (rel.RelTo < minDate)
                rel.RelTo = null;

            var relData = AngazovanostDataToRelationSimple(ang);
            rel.Descr = relData.Item2;
            if (string.IsNullOrEmpty(rel.Descr))
                rel.Descr = relData.Item1.ToNiceDisplayName();
            rel.UpdateAktualnost();
            return rel;
        }

        private static Tuple<HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum, string>
            AngazovanostDataToRelationSimple(AngazovanostData ang)
        {
            HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum relRelationship =
                HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jiny;
            string descr = ang.descr;
            /*
       3  - prokura
       4  - člen dozorčí rady
       24 - spolecnik
       5  - Jediný akcionář
       1 - jednatel
    */


            switch (ang.kod_ang)
            {
                case 1:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                    break;
                case 3:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Prokura.ToNiceDisplayName();
                    break;
                case 4:
                case 7:
                case 2:
                case 18:
                case 25:
                case 26:
                case 28:
                case 31:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();
                    break;
                case 33:
                case 34:
                case 35:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Zakladatel;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();
                    break;
                case 5:
                case 9:
                case 10:
                case 15:
                case 19:
                case 24:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Spolecnik;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Spolecnik.ToNiceDisplayName();
                    break;
                case 100:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Souhrnny;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                    break;
                case 23: //
                case 29: //
                case 11: //
                case 12: //
                case 13: //
                case 16: //
                case 17: //
                case 37: //
                case 40: //
                case 41: //
                case 42: //
                case 99:
                    relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jiny;
                    break;
                default:

                    if (ang.kod_ang < 0)
                    {
                        relRelationship = (HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum)ang.kod_ang;
                        descr = relRelationship.ToNiceDisplayName();
                    }
                    else
                    {
                        //rel.Relationship = Relation.RelationDescriptionEnum.Jednatel;
                        relRelationship = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jiny;
                        if (string.IsNullOrEmpty(descr))
                            descr = HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                    }

                    break;
            }

            return new Tuple<HlidacStatu.DS.Graphs.Relation.RelationSimpleEnum, string>(relRelationship, descr);
        }

        public static IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> GetDirectParentRelationsFirmy(string ico,
            Relation.CharakterVazbyEnum charakterVazby)
        {
            var charakterVazbyQuery = "";
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
            {
                charakterVazbyQuery =
                    $"and typVazby not in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }
            else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
            {
                charakterVazbyQuery = $"and typVazby in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }

            string sql = @"select ICO, vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from FirmaVazby 
            where vazbakico=@ico 
        ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<HlidacStatu.DS.Graphs.Graph.Edge> relations = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new DbConnect();
            var sqlCall = DirectDB.Instance.GetRawSql(sql, new SqlParameter[]
            {
                new SqlParameter("ico", ico)
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parents = ds.Tables[0].AsEnumerable()
                    .Where(m => (string)m["ico"] != ico)
                    .Select(dr => new AngazovanostData()
                    {
                        subjId = (string)dr["ico"],
                        subjname = "",
                        NodeType = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company,
                        fromDate = (DateTime?)DirectSql.IsNull(dr["datumOd"], null),
                        toDate = (DateTime?)DirectSql.IsNull(dr["datumDo"], null),
                        kod_ang = Convert.ToInt32(dr["typVazby"]),
                        descr = (string)DirectSql.IsNull(dr["PojmenovaniVazby"], ""),
                        podil = (decimal?)DirectSql.IsNull(dr["podil"], null)
                    })
                    .ToArray();
                var ret = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
                for (int i = 0; i < parents.Length; i++)
                {
                    AngazovanostData ang = parents[i];
                    var rel = AngazovanostDataToEdge(ang,
                        new HlidacStatu.DS.Graphs.Graph.Node()
                            { Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company, Id = ang.subjId },
                        new HlidacStatu.DS.Graphs.Graph.Node()
                            { Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company, Id = ico },
                        -1
                    );
                    ret.Add(rel);
                }

                return HlidacStatu.DS.Graphs.Graph.Edge.GetLongestEdges(ret);
            }

            return new HlidacStatu.DS.Graphs.Graph.Edge[] { };
        }

        public static IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> GetDirectParentRelationsOsoby(int osobaInternalId)
        {
            string sql = @"select OsobaID, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from osobavazby 
                        where VazbakOsobaId =@internalId
                    ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<HlidacStatu.DS.Graphs.Graph.Edge> relations = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new DbConnect();
            var sqlCall = DirectDB.Instance.GetRawSql(sql, new SqlParameter[]
            {
                new SqlParameter("internalId", osobaInternalId)
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parents = ds.Tables[0].AsEnumerable()
                    .Select(dr => new AngazovanostData()
                    {
                        subjId = ((int)dr["osobaId"]).ToString(),
                        subjname = "",
                        NodeType = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person,
                        fromDate = (DateTime?)DirectSql.IsNull(dr["datumOd"], null),
                        toDate = (DateTime?)DirectSql.IsNull(dr["datumDo"], null),
                        kod_ang = Convert.ToInt32(dr["typVazby"]),
                        descr = (string)DirectSql.IsNull(dr["PojmenovaniVazby"], ""),
                        podil = (decimal?)DirectSql.IsNull(dr["podil"], null)
                    })
                    .ToArray();
                var ret = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
                for (int i = 0; i < parents.Length; i++)
                {
                    AngazovanostData ang = parents[i];
                    var rel = AngazovanostDataToEdge(ang,
                        new HlidacStatu.DS.Graphs.Graph.Node()
                            { Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person, Id = ang.subjId },
                        new HlidacStatu.DS.Graphs.Graph.Node()
                        {
                            Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person, Id = osobaInternalId.ToString()
                        },
                        -1
                    );

                    ret.Add(rel);
                }

                return HlidacStatu.DS.Graphs.Graph.Edge.GetLongestEdges(ret);
            }

            return new HlidacStatu.DS.Graphs.Graph.Edge[] { };
        }

        public static IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> GetDirectParentRelationsOsoby(string ico)
        {
            string sql = @"select OsobaID, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from osobavazby 
                        where vazbakico=@ico 
                    ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<HlidacStatu.DS.Graphs.Graph.Edge> relations = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new DbConnect();
            var sqlCall = DirectDB.Instance.GetRawSql(sql, new SqlParameter[]
            {
                new SqlParameter("ico", ico)
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parents = ds.Tables[0].AsEnumerable()
                    .Select(dr => new AngazovanostData()
                    {
                        subjId = ((int)dr["osobaId"]).ToString(),
                        subjname = "",
                        NodeType = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company,
                        fromDate = (DateTime?)DirectSql.IsNull(dr["datumOd"], null),
                        toDate = (DateTime?)DirectSql.IsNull(dr["datumDo"], null),
                        kod_ang = Convert.ToInt32(dr["typVazby"]),
                        descr = (string)DirectSql.IsNull(dr["PojmenovaniVazby"], ""),
                        podil = (decimal?)DirectSql.IsNull(dr["podil"], null)
                    })
                    .ToArray();
                var ret = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
                for (int i = 0; i < parents.Length; i++)
                {
                    AngazovanostData ang = parents[i];
                    var rel = AngazovanostDataToEdge(ang,
                        new HlidacStatu.DS.Graphs.Graph.Node()
                            { Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person, Id = ang.subjId },
                        new HlidacStatu.DS.Graphs.Graph.Node()
                            { Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person, Id = ico },
                        -1
                    );

                    ret.Add(rel);
                }

                return HlidacStatu.DS.Graphs.Graph.Edge.GetLongestEdges(ret);
            }

            return new HlidacStatu.DS.Graphs.Graph.Edge[] { };
        }

        public static async Task<IEnumerable<DS.Graphs.Graph.Edge>> GetParentRelationsAsync(string ico,
            Relation.CharakterVazbyEnum charakterVazby,
            IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> currRelations, int distance,
            DateTime datumOd, DateTime datumDo)
        {
            var charakterVazbyQuery = "";
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
            {
                charakterVazbyQuery =
                    $"and typVazby not in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }
            else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
            {
                charakterVazbyQuery = $"and typVazby in ({string.Join(",", Relation.CharakterVazby_UredniVazbyIds)})";
            }

            string sql = @$"select ico, vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from Firmavazby 
            where vazbakico=@ico 
            {charakterVazbyQuery}
        	and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
        ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<HlidacStatu.DS.Graphs.Graph.Edge> relations =
                currRelations?.ToList() ?? new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new DbConnect();
            var sqlCall = DirectDB.Instance.GetRawSql(sql, new SqlParameter[]
            {
                new SqlParameter("ico", ico),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = await db.ExecuteDatasetAsync(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parentIcos = ds.Tables[0].AsEnumerable()
                    .Select(dr => (string)dr["ico"])
                    .Where(m => m != ico);


                foreach (var parentIco in parentIcos)
                {
                    var parentRels = await VsechnyDcerineVazbyInternalAsync(parentIco,
                        charakterVazby,
                        0, true, null,
                        new ExcludeDataCol() { items = relations?.Select(m => new ExcludeData(m)).ToList() }
                        , datumOd: datumOd, datumDo: datumDo
                    );
                    //move

                    //add edge with parent 
                    var currEdge = new HlidacStatu.DS.Graphs.Graph.Edge()
                    {
                        From = new HlidacStatu.DS.Graphs.Graph.Node() { },
                        To = new HlidacStatu.DS.Graphs.Graph.Node() { },
                        RelFrom = datumOd,
                        RelTo = datumDo,
                    };
                    currEdge.UpdateAktualnost();
                    relations.Add(currEdge);

                    if ((parentRels?.Count() ?? 0) > 0)
                    {
                        relations.AddRange(parentRels);
                    }
                }

                foreach (var parentIco in parentIcos)
                {
                    var parents = await GetParentRelationsAsync(parentIco, charakterVazby, relations, distance + 1, datumOd,
                        datumDo);
                    relations.AddRange(parents);
                }

                return relations;
            }

            return [];
        }


        private class AngazovanostData
        {
            public string subjId { get; set; }
            public string subjname { get; set; }
            public byte stav { get; set; }
            public DateTime? fromDate { get; set; }
            public DateTime? toDate { get; set; }
            public string descr { get; set; }
            public int kod_ang { get; set; }
            public decimal? podil { get; set; }
            public HlidacStatu.DS.Graphs.Graph.Node.NodeType NodeType { get; set; }
        }

        public class ExcludeData
        {
            public ExcludeData()
            {
            }

            public ExcludeData(HlidacStatu.DS.Graphs.Graph.Edge r)
            {
                parent = r.From;
                child = r.To;
                from = r.RelFrom;
                to = r.RelTo;
            }

            public HlidacStatu.DS.Graphs.Graph.Node parent { get; set; }
            public HlidacStatu.DS.Graphs.Graph.Node child { get; set; }
            public DateTime? from { get; set; }
            public DateTime? to { get; set; }

            public bool Mergable(ExcludeData newI)
            {
                if (newI.from.HasValue && to.HasValue
                                       && to < newI.from
                   )
                    return false;

                if (newI.to.HasValue && from.HasValue
                                     && from > newI.to
                   )
                    return false;

                if (newI.from == null && newI.to == null)
                    return true;

                //at least in one interval
                if (from.HasValue && to.HasValue)
                {
                    if (newI.from.HasValue && (newI.from >= from && newI.from <= to))
                        return true;
                    if (newI.to.HasValue && (newI.to >= from && newI.to <= to))
                        return true;
                }
                else if (from.HasValue == false && to.HasValue)
                {
                    if (newI.from.HasValue && (newI.from <= to))
                        return true;
                    if (newI.to.HasValue && (newI.to <= to))
                        return true;
                }
                else if (from.HasValue && to.HasValue == false)
                {
                    if (newI.from.HasValue && (newI.from >= from))
                        return true;
                    if (newI.to.HasValue && (newI.to >= from))
                        return true;
                }
                else if (from.HasValue == false && to.HasValue == false)
                    return true;

                return false;
            }

            public void Merge(ExcludeData i)
            {
                if (i.from == null)
                    from = i.from;
                if (i.to == null)
                    to = i.to;

                if (from.HasValue && i.from.HasValue && i.from < from)
                    from = i.from;
                if (to.HasValue && i.to.HasValue && i.to > to)
                    to = i.to;
            }
        }

        public class ExcludeDataCol
        {
            public List<ExcludeData> items { get; set; } = new List<ExcludeData>();

            public ExcludeDataCol AddItem(ExcludeData i)
            {
                var icoItems = items.Where(m => m.parent == i.parent && m.child == i.child);
                if (items.Count() > 0)
                {
                    foreach (var it in icoItems)
                    {
                        if (it.Mergable(i))
                        {
                            it.Merge(i);
                            return this;
                        }
                    }

                    items.Add(i);
                }
                else
                    items.Add(i);

                return this;
            }

            public ExcludeDataCol AddItems(IEnumerable<ExcludeData> items)
            {
                foreach (var i in items)
                    AddItem(i);

                return this;
            }

            public bool Contains(HlidacStatu.DS.Graphs.Graph.Edge r)
            {
                var ex = new ExcludeData(r);
                return items
                    .Where(m => m.parent?.UniqId == ex.parent?.UniqId && m.child?.UniqId == ex.child?.UniqId)
                    //datum se prekryva
                    .Any(m => m.Mergable(ex));
            }
        }


        public static async Task<string> TiskVazebAsync(string rootName, HlidacStatu.DS.Graphs.Graph.Edge origRoot,
            IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> vazby, Relation.TiskEnum typ, bool withStats = true)
        {
            string htmlTemplate = "<ul id='nestedlist'><li>{0}</li>{1}</ul>";
            string textTemplate = "{0}\n|\n{1}";
            string jsonTemplate = "{{ \"text\": {{ \"name\": \"{0}\" }}, \"children\": [ {1} ] }}";
            string checkboxTemplate = "<ul>{0} {1}</ul>";
            switch (typ)
            {
                case Relation.TiskEnum.Text:
                    return string.Format(textTemplate, rootName,
                        await PrintFlatRelationsAsync(origRoot, (HlidacStatu.DS.Graphs.Graph.Edge)null, 0, vazby, typ,
                            null, withStats));
                case Relation.TiskEnum.Html:
                    return string.Format(htmlTemplate, rootName,
                        await PrintFlatRelationsAsync(origRoot, (HlidacStatu.DS.Graphs.Graph.Edge)null, 0, vazby, typ,
                            null, withStats));
                case Relation.TiskEnum.Json:
                    return string.Format(jsonTemplate, rootName,
                        await PrintFlatRelationsAsync(origRoot, (HlidacStatu.DS.Graphs.Graph.Edge)null, 0, vazby, typ,
                            null, withStats));
                case Relation.TiskEnum.Checkbox:
                    return string.Format(checkboxTemplate, rootName,
                        await PrintFlatRelationsAsync(origRoot, (HlidacStatu.DS.Graphs.Graph.Edge)null, 0, vazby, typ,
                            null, withStats));
                default:
                    return string.Empty;
            }
        }

        private static async Task<string> PrintFlatRelationsAsync(HlidacStatu.DS.Graphs.Graph.Edge origRoot,
            HlidacStatu.DS.Graphs.Graph.Edge parent, int level, IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> relations,
            Relation.TiskEnum typ,
            List<string> renderedIds, bool withStats = true, string highlightSubjId = null)
        {
            if (parent == null)
                return await PrintFlatRelationsAsync(origRoot, (HlidacStatu.DS.Graphs.Graph.MergedEdge)null, level,
                    relations, typ,
                    renderedIds, withStats, highlightSubjId);
            else
                return await PrintFlatRelationsAsync(origRoot, new HlidacStatu.DS.Graphs.Graph.MergedEdge(parent),
                    level, relations, typ,
                    renderedIds, withStats, highlightSubjId);
        }

        private static async Task<string> PrintFlatRelationsAsync(HlidacStatu.DS.Graphs.Graph.Edge origRoot,
            HlidacStatu.DS.Graphs.Graph.MergedEdge parent, int level,
            IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> relations, Relation.TiskEnum typ,
            List<string> renderedIds, bool withStats = true, string highlightSubjId = null)
        {
            int space = 2;
            string horizLine = "--"; //new string('\u2500',2);
            string vertLine = "|"; //new string('\u2502',1);
            string cross = "+"; //new string('\u251C', 1);

            if (renderedIds == null)
                renderedIds = new List<string>();

            IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> firstBatch = null;
            if (parent == null)
                firstBatch = relations.Where(m =>
                    !relations.Any(r => r.To?.UniqId == m.From?.UniqId)
                    || m.From?.UniqId == origRoot.To?.UniqId
                );
            else
                firstBatch = relations.Where(m => m.From?.UniqId == parent.To?.UniqId);

            
            
            var mergedGroups = firstBatch
                .Distinct()
                .GroupBy(k => new { id = k.To.UniqId, type = k.To.Type }, (k, v) =>
                {
                    var withChildren = HlidacStatu.DS.Graphs.Graph.Edge.MergeSameEdges(v);
                    return withChildren ?? new HlidacStatu.DS.Graphs.Graph.MergedEdge(v.First());
                })
                .ToArray();
            
            var edgesWithNames = new List<(HlidacStatu.DS.Graphs.Graph.MergedEdge Edge, string SortName)>();
            foreach (var edge in mergedGroups)
            {
                var name = await edge.To.PrintNameAsync();
                edgesWithNames.Add((edge, name));
            }

            var rels = edgesWithNames
                .OrderBy(x => x.SortName)
                .Select(x => x.Edge)
                .ToArray();
            

            if (!rels.Any())
                return string.Empty;

            StringBuilder sb = new StringBuilder(512);
            List<HlidacStatu.DS.Graphs.Graph.Edge> deepRels = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            switch (typ)
            {
                case Relation.TiskEnum.Text:
                    break;
                case Relation.TiskEnum.Html:
                case Relation.TiskEnum.Checkbox:
                    sb.AppendLine("<ul>");
                    break;
                case Relation.TiskEnum.Json:
                    break;
            }

            for (int i = 0; i < rels.Count(); i++)
            {
                var rel = rels[i];
                if (renderedIds.Contains(rel.To.UniqId))
                    continue;

                var last = i == (rels.Count() - 1);
                StatisticsSubjectPerYear<Smlouva.Statistics.Data> stat = null;
                if (withStats && rel.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                    stat = await (await FirmaCache.GetAsync(rel.To.Id))
                        .StatistikaRegistruSmluvAsync(); //new Analysis.SubjectStatistic(rel.To.Id);

                string subjId = rel.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company ? rel.To.Id : "Osoba";
                string subjName = await rel.To.PrintNameAsync();
                renderedIds.Add(rel.To.UniqId);
                switch (typ)
                {
                    case Relation.TiskEnum.Text:
                        sb.AppendLine(string.Concat(Enumerable.Repeat(vertLine + new string(' ', space), level + 1)));
                        sb.Append(
                            string.Concat(
                                Enumerable.Repeat(
                                    vertLine + new string(' ', space)
                                    , (level))
                            )
                        );
                        if (rel.To.Highlighted)
                            subjName = string.Format("!!{0}!!", subjName);

                        sb.AppendFormat("{0}{1}:{2} {3}\n",
                            cross + horizLine + " ",
                            subjId,
                            subjName,
                            rel.Doba()
                        );
                        sb.Append(await PrintFlatRelationsAsync(origRoot, rel, level + 1, relations, typ, renderedIds,
                            withStats));
                        break;
                    case Relation.TiskEnum.Html:
                        if (withStats && stat != null)
                            sb.AppendFormat(
                                "<li class='{3} {6}'><a href='/subjekt/{0}'>{0}:{1}</a>{7}; {4}, celkem {5}. {2}</li>",
                                subjId,
                                subjName,
                                await PrintFlatRelationsAsync(origRoot, rel, level + 1, relations, typ, renderedIds,
                                    withStats),
                                last ? "" : "connect",
                                Devmasters.Lang.CS.Plural.Get(stat.Summary().PocetSmluv, Util.Consts.csCulture,
                                    "{0} smlouva", "{0} smlouvy", "{0} smluv"),
                                Smlouva.NicePrice(stat.Summary().CelkovaHodnotaSmluv, html: true, shortFormat: true),
                                "aktualnost" + ((int)rel.Aktualnost).ToString(),
                                (rel.Aktualnost < Relation.AktualnostType.Aktualni)
                                    ? rel.Doba(format: "/{0}/")
                                    : string.Empty
                            );
                        else
                            sb.AppendFormat(
                                "<li class='{3} {4}'><a href='/subjekt/{0}'><span class=''>{0}:{1}</span></a>{5}.  {2}</li>",
                                subjId,
                                subjName,
                                await PrintFlatRelationsAsync(origRoot, rel, level + 1, relations, typ, renderedIds,
                                    withStats),
                                last ? "" : "connect",
                                "aktualnost" + ((int)rel.Aktualnost).ToString(),
                                (rel.Aktualnost < Relation.AktualnostType.Aktualni)
                                    ? rel.Doba(format: "/{0}/")
                                    : string.Empty,
                                (!string.IsNullOrEmpty(highlightSubjId) && subjId == highlightSubjId)
                                    ? "highlighted"
                                    : ""
                            );

                        break;
                    case Relation.TiskEnum.Checkbox:
                        sb.AppendFormat(
                            @"<li class=""{0} {1}""><input type=""checkbox"" name=""ico"" value=""{2}"" /> <span><b>{2}</b> {3}</span>{4}</li>"
                            , (last ? "" : "connect"),
                            ("aktualnost" + ((int)rel.Aktualnost).ToString()),
                            subjId, subjName,
                            await PrintFlatRelationsAsync(origRoot, rel, level + 1, relations, typ, renderedIds,
                                withStats)
                        );

                        break;
                    case Relation.TiskEnum.Json:
                        break;
                }
            }

            switch (typ)
            {
                case Relation.TiskEnum.Text:
                    break;
                case Relation.TiskEnum.Html:
                case Relation.TiskEnum.Checkbox:
                    sb.AppendLine("</ul>");
                    break;
                case Relation.TiskEnum.Json:
                    break;
            }

            return sb.ToString();
        }

        public static async Task<string> PrintName2Async(this HlidacStatu.DS.Graphs.Graph.Node node, bool html = false)
        {
            switch (node.Type)
            {
                case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person:
                    return (await OsobaCache.GetPersonByIdAsync(Convert.ToInt32(node.Id)))?.FullNameWithYear(html) ?? "(neznámá osoba)";
                case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company:
                default:
                    return await FirmaCache.GetJmenoAsync(node.Id);
            }
        }

        public static async Task<string> ExportTabDataAsync(IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> data)
        {
            if (data == null)
                return "";
            if (data.Count() == 0)
                return "";

            StringBuilder sb = new StringBuilder(1024);

            foreach (var i in data)
            {
                string fromName = "";
                string toName = "";
                
                if (i.From != null)
                    fromName = await i.From.PrintNameAsync();
                
                if (i.To != null)
                    toName = await i.To.PrintNameAsync();
                    
                sb.AppendFormat(
                    $"{i.From?.Id}\t{fromName}\t{i.To?.Id}\t{toName}\t{i.RelFrom?.ToShortDateString() ?? "Ø"}\t{i.RelTo?.ToShortDateString() ?? "Ø"}\t{i.Descr}\n");
            }

            return sb.ToString();
        }

        public static async Task<string> ExportGraphJsonDataAsync(IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> data)
        {
            if (data == null)
                return "{\"nodes\": [],\"edges\": []}";
            if (data.Count() == 0)
                return "{\"nodes\": [],\"edges\": []}";

            Dictionary<string, Graph.GraphJson> nodes = new Dictionary<string, Graph.GraphJson>();

            foreach (var i in data)
            {
                if (i.From != null && !nodes.ContainsKey(i.From.UniqId))
                {
                    nodes.Add(i.From.UniqId, await Graph.GraphJson.CreateAsync(i.From, i.Distance - 1, i.Distance == 0));
                }

                if (i.To != null && !nodes.ContainsKey(i.To.UniqId))
                    nodes.Add(i.To.UniqId, await Graph.GraphJson.CreateAsync(i.To, i.Distance));
            }

            var ret = new
            {
                nodes = nodes.Select(m => m.Value).ToArray(),
                edges = data
                    .Where(e => e.To != null && e.From != null)
                    .Select(e => new Graph.GraphJson(e)).ToArray()
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(ret);
        }

        public static async Task<string> PrintNameAsync(this HlidacStatu.DS.Graphs.Graph.Node node, bool html = false)
        {
            switch (node.Type)
            {
                case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person:
                    return (await OsobaCache.GetPersonByIdAsync(Convert.ToInt32(node.Id)))?.FullNameWithYear(html) ?? "(neznámá osoba)";
                case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company:
                default:
                    return await FirmaCache.GetJmenoAsync(node.Id);
            }
        }
    }
}