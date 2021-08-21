using Devmasters;
using Devmasters.Enums;
using HlidacStatu.Util.Cache;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;


namespace HlidacStatu.Repositories
{
    public static partial class Graph
    {
        static DateTime minDate = new DateTime(1950, 1, 1);

        public static IEnumerable<string> UniqIds(IEnumerable<Datastructures.Graphs.Graph.Edge> edges)
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

        private static CouchbaseCacheManager<List<Datastructures.Graphs.Graph.Edge>, string> vazbyIcoCache
            = CouchbaseCacheManager<List<Datastructures.Graphs.Graph.Edge>, string>.GetSafeInstance("VsechnyDcerineVazby",
                ico => vsechnyDcerineVazbyInternal(ico, 0, true, null),
                TimeSpan.FromDays(3),
                Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        private static CouchbaseCacheManager<List<Datastructures.Graphs.Graph.Edge>, string> vazbyOsobaNameIdCache
            = CouchbaseCacheManager<List<Datastructures.Graphs.Graph.Edge>, string>.GetSafeInstance("VsechnyDcerineVazbyOsoba",
                osobaNameId =>
                {
                    if (string.IsNullOrEmpty(osobaNameId))
                        return new List<Datastructures.Graphs.Graph.Edge>();
                    Osoba o = Osoby.GetByNameId.Get(osobaNameId);
                    return vsechnyDcerineVazbyInternal(o, 0, true, null);
                },
                TimeSpan.FromDays(3), Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        public static List<Datastructures.Graphs.Graph.Edge> VsechnyDcerineVazby(string ico, bool refresh = false)
        {
            if (refresh)
                vazbyIcoCache.Delete(ico);
            return vazbyIcoCache.Get(ico);
        }

        public static List<Datastructures.Graphs.Graph.Edge> VsechnyDcerineVazby(Osoba person, bool refresh = false)
        {
            if (refresh)
                vazbyOsobaNameIdCache.Delete(person?.NameId);
            return vazbyOsobaNameIdCache.Get(person?.NameId);
        }

        public static List<Datastructures.Graphs.Graph.Edge> vsechnyDcerineVazbyInternal(string ico, int level, bool goDeep, Datastructures.Graphs.Graph.Edge parent,
            ExcludeDataCol excludeICO = null, DateTime? datumOd = null, DateTime? datumDo = null, decimal minPodil = 0,
            Relation.AktualnostType aktualnost = Relation.AktualnostType.Libovolny)
        {
            string sql = $@"select vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from Firmavazby 
    where ico=@ico 
    and (podil is null or podil >= {minPodil})
	and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
";
            var p = new IDataParameter[]
            {
                new SqlParameter("ico", ico),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            };

            var rel = GetChildrenRelations(sql, Datastructures.Graphs.Graph.Node.NodeType.Company, ico, datumOd, datumDo,
                p, level, goDeep, parent, excludeICO, minPodil, aktualnost);
            return rel;
        }


        public static List<Datastructures.Graphs.Graph.Edge> vsechnyDcerineVazbyInternal(Osoba person, int level, bool goDeep, Datastructures.Graphs.Graph.Edge parent,
            ExcludeDataCol excludeICO = null, IEnumerable<int> excludeOsobaId = null,
            DateTime? datumOd = null, DateTime? datumDo = null, decimal minPodil = 0,
            Relation.AktualnostType aktualnost = Relation.AktualnostType.Libovolny)
        {
            if (excludeOsobaId == null)
                excludeOsobaId = new int[] { };

            string sql =
                $@"select vazbakIco, vazbakOsobaId, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from OsobaVazby 
                            where osobaId = @osobaId 
                            and (podil is null or podil >= {minPodil})
                	        and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
";
            var p = new IDataParameter[]
            {
                new SqlParameter("osobaId", person.InternalId),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            };

            var relForPerson = GetChildrenRelations(sql, Datastructures.Graphs.Graph.Node.NodeType.Person, person.InternalId.ToString(),
                datumOd, datumDo,
                p, level, goDeep, parent, excludeICO, minPodil, aktualnost);

            var relForConnectedPersons = new List<Datastructures.Graphs.Graph.Edge>();
            using (DbEntities db = new DbEntities())
            {
                var navazaneOsoby = db.OsobaVazby.AsQueryable()
                    .Where(m => m.OsobaId == person.InternalId && m.VazbakOsobaId != null).ToList();
                if (navazaneOsoby.Count > 0)
                    foreach (var ov in navazaneOsoby)
                    {
                        Datastructures.Graphs.Graph.Edge parentRelFound = relForPerson
                            .Where(r => r.To.Type == Datastructures.Graphs.Graph.Node.NodeType.Person
                                        && r.To.Id == ov.VazbakOsobaId.Value.ToString())
                            .FirstOrDefault();

                        if (!excludeOsobaId.Contains(ov.VazbakOsobaId.Value))
                        {
                            Osoba o = Osoby.GetById.Get(ov.VazbakOsobaId.Value);
                            excludeOsobaId = excludeOsobaId.Union(new int[] {ov.VazbakOsobaId.Value});
                            var rel = vsechnyDcerineVazbyInternal(o, level + 1, true, parentRelFound,
                                excludeOsobaId: excludeOsobaId, aktualnost: aktualnost);
                            relForConnectedPersons = Datastructures.Graphs.Graph.Edge.Merge(relForConnectedPersons, rel);
                        }
                    }
            }

            var finalRel = Datastructures.Graphs.Graph.Edge.Merge(relForConnectedPersons, relForPerson);
            return finalRel;
        }

        public static List<Datastructures.Graphs.Graph.Edge> Holding(string ico, Relation.AktualnostType aktualnost)
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

            return Holding(ico, from.Value, to, aktualnost);
        }

        public static List<Datastructures.Graphs.Graph.Edge> Holding(string ico, DateTime datumOd, DateTime datumDo,
            Relation.AktualnostType aktualnost)
        {
            var vazby = vsechnyDcerineVazbyInternal(ico, 9, true, null, datumOd: datumOd, datumDo: datumDo);
            var parents = GetParentRelations(ico, vazby, 0, datumOd, datumDo);
            var rootNode = new Datastructures.Graphs.Graph.Node() {Id = ico, Type = Datastructures.Graphs.Graph.Node.NodeType.Company};
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

            var finalRel = Datastructures.Graphs.Graph.Edge.Merge(parents.ToList(), vazby);
            return finalRel;
        }

        public static List<Datastructures.Graphs.Graph.Edge> GetChildrenRelations(string sql,
            Datastructures.Graphs.Graph.Node.NodeType nodeType, string nodeId, DateTime? datumOd, DateTime? datumDo,
            IDataParameter[] parameters, int level, bool goDeep,
            Datastructures.Graphs.Graph.Edge parent, ExcludeDataCol excludeICO, decimal minPodil, Relation.AktualnostType aktualnost)
        {
            if (excludeICO == null)
                excludeICO = new ExcludeDataCol();

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<Datastructures.Graphs.Graph.Edge> relations = new List<Datastructures.Graphs.Graph.Edge>();
            if (level == 0 && parent == null)
            {
                //add root node / edge
                relations.Add(
                    new Datastructures.Graphs.Graph.Edge()
                    {
                        From = null,
                        Root = true,
                        To = new Datastructures.Graphs.Graph.Node() {Id = nodeId, Type = nodeType},
                        RelFrom = datumOd,
                        RelTo = datumDo,
                        Distance = 0
                    }
                );
            }
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new PersistLib();
            var sqlCall = DirectDB.GetRawSql(CommandType.Text, sql, parameters);
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                List<AngazovanostData> rows = new List<AngazovanostData>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    AngazovanostData angaz = null;
                    var ico = (string) dr["VazbakIco"];
                    if (string.IsNullOrEmpty(ico))
                    {
                        if (dr.Table.Columns.Contains("vazbakOsobaId"))
                        {
                            var vazbakOsobaId = (int?) PersistLib.IsNull(dr["vazbakOsobaId"], null);
                            if (vazbakOsobaId != null)
                            {
                                Osoba o = Osoby.GetById.Get(vazbakOsobaId.Value);
                                angaz = new AngazovanostData()
                                {
                                    subjId = vazbakOsobaId.Value.ToString(),
                                    NodeType = Datastructures.Graphs.Graph.Node.NodeType.Person,
                                    fromDate = (DateTime?) PersistLib.IsNull(dr["datumOd"], null),
                                    toDate = (DateTime?) PersistLib.IsNull(dr["datumDo"], null),
                                    kod_ang = Convert.ToInt32(dr["typVazby"]),
                                    descr = (string) PersistLib.IsNull(dr["PojmenovaniVazby"], ""),
                                    podil = (decimal?) PersistLib.IsNull(dr["podil"], null)
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
                            NodeType = Datastructures.Graphs.Graph.Node.NodeType.Company,
                            fromDate = (DateTime?) PersistLib.IsNull(dr["datumOd"], null),
                            toDate = (DateTime?) PersistLib.IsNull(dr["datumDo"], null),
                            kod_ang = Convert.ToInt32(dr["typVazby"]),
                            descr = (string) PersistLib.IsNull(dr["PojmenovaniVazby"], ""),
                            podil = (decimal?) PersistLib.IsNull(dr["podil"], null)
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
                                    && rr.fromDate < r.fromDate
                                    && (rr.toDate > r.toDate || rr.toDate.HasValue == false)
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
                            relations.Any(
                                r => r.To.Id == ang.subjId
                                     && r.To.Type == ang.NodeType
                                     && r.RelFrom == ang.fromDate
                                     && r.RelTo == ang.toDate
                            )
                        )
                            continue;
                    }

                    var rel = AngazovanostDataToEdge(ang,
                        new Datastructures.Graphs.Graph.Node() {Type = nodeType, Id = nodeId},
                        new Datastructures.Graphs.Graph.Node() {Type = ang.NodeType, Id = ang.subjId},
                        level + 1
                    );


                    if (excludeICO.Contains(rel))
                        continue; //skip to the next

                    if (rel.Aktualnost >= aktualnost)
                        relations.Add(rel);
                }
            }

            if (goDeep && relations.Count > 0)
            {
                level++;
                List<Datastructures.Graphs.Graph.Edge> deeperRels = new List<Datastructures.Graphs.Graph.Edge>();
                List<Datastructures.Graphs.Graph.Edge> excludeMore = new List<Datastructures.Graphs.Graph.Edge>();
                if (parent != null)
                {
                    excludeMore = relations.ToList();
                }

                ////navazej na ten, ktery je nejdelsi
                //var parentRels = relations.GroupBy(x => new { x.To.Id }, (key, rels) =>
                //{
                //    DateTime? fromDate = rels.Any(m => m.FromDate == null) ? (DateTime?)null : rels.Min(m => m.FromDate);
                //    DateTime? toDate = rels.Any(m => m.ToDate == null) ? (DateTime?)null : rels.Max(m => m.ToDate);
                //    Relation bestRelation = Relation.GetLongestRelation(rels);

                //    return new
                //    {
                //        SubjIco = key.To.Id,
                //        FromDate = fromDate,
                //        ToDate = toDate,
                //        BestRelation = bestRelation,
                //    };

                //});


                foreach (var rel in relations.Where(m => m.Root == false))
                {
                    //old
                    deeperRels.AddRange(
                        vsechnyDcerineVazbyInternal(rel.To.Id, level, goDeep, rel,
                            excludeICO.AddItem(new ExcludeData(rel)),
                            rel.RelFrom, rel.RelTo, minPodil, aktualnost)
                    );
                }

                relations.AddRange(deeperRels);
            }

            if (level == 0)
            {
                //remove inactive companies from last branches
                //TODO
            }

            return relations;
        }

        private static Datastructures.Graphs.Graph.Edge AngazovanostDataToEdge(AngazovanostData ang, Datastructures.Graphs.Graph.Node fromNode, Datastructures.Graphs.Graph.Node toNode, int distance)
        {
            var rel = new Datastructures.Graphs.Graph.Edge();
            rel.From = fromNode;
            rel.To = toNode;
            rel.Distance = distance;
            rel.VazbaType = ang.kod_ang;
            rel.RelFrom = (DateTime?) PersistLib.IsNull(ang.fromDate, null);
            if (rel.RelFrom < minDate)
                rel.RelFrom = null;

            rel.RelTo = (DateTime?) PersistLib.IsNull(ang.toDate, null);
            if (rel.RelTo < minDate)
                rel.RelTo = null;

            var relData = AngazovanostDataToRelationSimple(ang);
            rel.Descr = relData.Item2;
            if (string.IsNullOrEmpty(rel.Descr))
                rel.Descr = relData.Item1.ToNiceDisplayName();
            rel.UpdateAktualnost();
            return rel;
        }

        private static Tuple<HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum, string> AngazovanostDataToRelationSimple(AngazovanostData ang)
        {
            HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jiny;
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
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                    break;
                case 3:
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Prokura.ToNiceDisplayName();
                    break;
                case 4:
                case 7:
                case 2:
                case 18:
                case 25:
                case 26:
                case 28:
                case 31:
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Statutarni_organ;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();
                    break;
                case 33:
                case 34:
                case 35:
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Zakladatel;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();
                    break;
                case 5:
                case 9:
                case 10:
                case 15:
                case 19:
                case 24:
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Spolecnik;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Spolecnik.ToNiceDisplayName();
                    break;
                case 100:
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Souhrnny;
                    if (string.IsNullOrEmpty(descr))
                        descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
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
                    relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jiny;
                    break;
                default:

                    if (ang.kod_ang < 0)
                    {
                        relRelationship = (HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum)ang.kod_ang;
                        descr = relRelationship.ToNiceDisplayName();
                    }
                    else
                    {
                        //rel.Relationship = Relation.RelationDescriptionEnum.Jednatel;
                        relRelationship = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jiny;
                        if (string.IsNullOrEmpty(descr))
                            descr = HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                    }

                    break;
            }

            return new Tuple<HlidacStatu.Datastructures.Graphs.Relation.RelationSimpleEnum, string>(relRelationship, descr);
        }

        public static IEnumerable<Datastructures.Graphs.Graph.Edge> GetDirectParentRelationsFirmy(string ico)
        {
            string sql = @"select ICO, vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from FirmaVazby 
            where vazbakico=@ico 
        ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<Datastructures.Graphs.Graph.Edge> relations = new List<Datastructures.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new PersistLib();
            var sqlCall = DirectDB.GetRawSql(CommandType.Text, sql, new IDataParameter[]
            {
                new SqlParameter("ico", ico)
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parents = ds.Tables[0].AsEnumerable()
                    .Where(m => (string) m["ico"] != ico)
                    .Select(dr => new AngazovanostData()
                    {
                        subjId = (string) dr["ico"],
                        subjname = "",
                        NodeType = Datastructures.Graphs.Graph.Node.NodeType.Company,
                        fromDate = (DateTime?) PersistLib.IsNull(dr["datumOd"], null),
                        toDate = (DateTime?) PersistLib.IsNull(dr["datumDo"], null),
                        kod_ang = Convert.ToInt32(dr["typVazby"]),
                        descr = (string) PersistLib.IsNull(dr["PojmenovaniVazby"], ""),
                        podil = (decimal?) PersistLib.IsNull(dr["podil"], null)
                    })
                    .ToArray();
                var ret = new List<Datastructures.Graphs.Graph.Edge>();
                for (int i = 0; i < parents.Length; i++)
                {
                    AngazovanostData ang = parents[i];
                    var rel = AngazovanostDataToEdge(ang,
                        new Datastructures.Graphs.Graph.Node() {Type = Datastructures.Graphs.Graph.Node.NodeType.Company, Id = ang.subjId},
                        new Datastructures.Graphs.Graph.Node() {Type = Datastructures.Graphs.Graph.Node.NodeType.Company, Id = ico},
                        -1
                    );
                    ret.Add(rel);
                }

                return Datastructures.Graphs.Graph.Edge.GetLongestEdges(ret);
            }

            return new Datastructures.Graphs.Graph.Edge[] { };
        }

        public static IEnumerable<Datastructures.Graphs.Graph.Edge> GetDirectParentRelationsOsoby(string ico)
        {
            string sql = @"select OsobaID, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from osobavazby 
                        where vazbakico=@ico 
                    ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<Datastructures.Graphs.Graph.Edge> relations = new List<Datastructures.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new PersistLib();
            var sqlCall = DirectDB.GetRawSql(CommandType.Text, sql, new IDataParameter[]
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
                        subjId = ((int) dr["osobaId"]).ToString(),
                        subjname = "",
                        NodeType = Datastructures.Graphs.Graph.Node.NodeType.Company,
                        fromDate = (DateTime?) PersistLib.IsNull(dr["datumOd"], null),
                        toDate = (DateTime?) PersistLib.IsNull(dr["datumDo"], null),
                        kod_ang = Convert.ToInt32(dr["typVazby"]),
                        descr = (string) PersistLib.IsNull(dr["PojmenovaniVazby"], ""),
                        podil = (decimal?) PersistLib.IsNull(dr["podil"], null)
                    })
                    .ToArray();
                var ret = new List<Datastructures.Graphs.Graph.Edge>();
                for (int i = 0; i < parents.Length; i++)
                {
                    AngazovanostData ang = parents[i];
                    var rel = AngazovanostDataToEdge(ang,
                        new Datastructures.Graphs.Graph.Node() {Type = Datastructures.Graphs.Graph.Node.NodeType.Person, Id = ang.subjId},
                        new Datastructures.Graphs.Graph.Node() {Type = Datastructures.Graphs.Graph.Node.NodeType.Person, Id = ico},
                        -1
                    );

                    ret.Add(rel);
                }

                return Datastructures.Graphs.Graph.Edge.GetLongestEdges(ret);
            }

            return new Datastructures.Graphs.Graph.Edge[] { };
        }

        public static IEnumerable<Datastructures.Graphs.Graph.Edge> GetParentRelations(string ico,
            IEnumerable<Datastructures.Graphs.Graph.Edge> currRelations, int distance,
            DateTime datumOd, DateTime datumDo)
        {
            string sql = @"select ico, vazbakIco, datumOd, datumDo, typVazby, pojmenovaniVazby, podil from Firmavazby 
            where vazbakico=@ico 
        	and dbo.IsSomehowInInterval(@datumOd, @datumDo, datumOd, datumDo) = 1
        ";

            string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");

            List<Datastructures.Graphs.Graph.Edge> relations = currRelations?.ToList() ?? new List<Datastructures.Graphs.Graph.Edge>();
            //get zakladni informace o subj.

            //find politician in the DB
            var db = new PersistLib();
            var sqlCall = DirectDB.GetRawSql(CommandType.Text, sql, new IDataParameter[]
            {
                new SqlParameter("ico", ico),
                new SqlParameter("datumOd", datumOd),
                new SqlParameter("datumDo", datumDo),
            });
            //string sqlFirma = "select top 1 stav_subjektu from firma where ico = @ico";

            var ds = db.ExecuteDataset(cnnStr, CommandType.Text, sqlCall, null);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var parentIcos = ds.Tables[0].AsEnumerable()
                    .Select(dr => (string) dr["ico"])
                    .Where(m => m != ico);


                foreach (var parentIco in parentIcos)
                {
                    var parentRels = vsechnyDcerineVazbyInternal(parentIco, 0, true, null,
                        new ExcludeDataCol() {items = relations?.Select(m => new ExcludeData(m)).ToList()}
                        , datumOd: datumOd, datumDo: datumDo
                    );
                    //move

                    //add edge with parent 
                    var currEdge = new Datastructures.Graphs.Graph.Edge()
                    {
                        From = new Datastructures.Graphs.Graph.Node() { },
                        To = new Datastructures.Graphs.Graph.Node() { },
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
                    var parents = GetParentRelations(parentIco, relations, distance + 1, datumOd, datumDo);
                    relations.AddRange(parents);
                }

                return relations;
            }

            return new Datastructures.Graphs.Graph.Edge[] { };
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
            public Datastructures.Graphs.Graph.Node.NodeType NodeType { get; set; }
        }

        public class ExcludeData
        {
            public ExcludeData()
            {
            }

            public ExcludeData(Datastructures.Graphs.Graph.Edge r)
            {
                parent = r.From;
                child = r.To;
                from = r.RelFrom;
                to = r.RelTo;
            }

            public Datastructures.Graphs.Graph.Node parent { get; set; }
            public Datastructures.Graphs.Graph.Node child { get; set; }
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

            public bool Contains(Datastructures.Graphs.Graph.Edge r)
            {
                var ex = new ExcludeData(r);
                return items
                    .Where(m => m.parent?.UniqId == ex.parent?.UniqId && m.child?.UniqId == ex.child?.UniqId)
                    .Any(m => m.Mergable(ex));
            }
        }
        
        
        public static string TiskVazeb(string rootName, IEnumerable<Datastructures.Graphs.Graph.Edge> vazby, Relation.TiskEnum typ, bool withStats = true)
        {
            string htmlTemplate = "<ul id='nestedlist'><li>{0}</li>{1}</ul>";
            string textTemplate = "{0}\n|\n{1}";
            string jsonTemplate = "{{ \"text\": {{ \"name\": \"{0}\" }}, \"children\": [ {1} ] }}";
            string checkboxTemplate = "<ul>{0} {1}</ul>";
            switch (typ)
            {
                case Relation.TiskEnum.Text:
                    return string.Format(textTemplate, rootName, PrintFlatRelations((Datastructures.Graphs.Graph.Edge)null, 0, vazby, typ, null, withStats));
                case Relation.TiskEnum.Html:
                    return string.Format(htmlTemplate, rootName, PrintFlatRelations((Datastructures.Graphs.Graph.Edge)null, 0, vazby, typ, null, withStats));
                case Relation.TiskEnum.Json:
                    return string.Format(jsonTemplate, rootName, PrintFlatRelations((Datastructures.Graphs.Graph.Edge)null, 0, vazby, typ, null, withStats));
                case Relation.TiskEnum.Checkbox:
                    return string.Format(checkboxTemplate, rootName, PrintFlatRelations((Datastructures.Graphs.Graph.Edge)null, 0, vazby, typ, null, withStats));
                default:
                    return string.Empty;
            }

        }

        private static string PrintFlatRelations(Datastructures.Graphs.Graph.Edge parent, int level, IEnumerable<Datastructures.Graphs.Graph.Edge> relations, Relation.TiskEnum typ,
           List<string> renderedIds, bool withStats = true, string highlightSubjId = null)
        {
            if (parent == null)
                return PrintFlatRelations((Datastructures.Graphs.Graph.MergedEdge)null, level, relations, typ,
                    renderedIds, withStats, highlightSubjId);
            else
                return PrintFlatRelations(new Datastructures.Graphs.Graph.MergedEdge(parent), level, relations, typ,
                renderedIds, withStats, highlightSubjId);
        }
        
        private static string PrintFlatRelations(Datastructures.Graphs.Graph.MergedEdge parent, int level, 
            IEnumerable<Datastructures.Graphs.Graph.Edge> relations, Relation.TiskEnum typ,
       List<string> renderedIds, bool withStats = true, string highlightSubjId = null)
        {
            int space = 2;
            string horizLine = "--";//new string('\u2500',2);
            string vertLine = "|";//new string('\u2502',1);
            string cross = "+"; //new string('\u251C', 1);

            if (renderedIds == null)
                renderedIds = new List<string>();

            var rels = relations 
                .Where(m =>
                    (
                    (parent != null && m.From?.UniqId == parent.To?.UniqId)
                    ||
                    (parent == null && !relations.Any(r => r.To?.UniqId == m.From?.UniqId)) //do root urovne pridej vazby, ktere jsou sirotci bez parenta
                    )
                )
                .Distinct()
                .GroupBy(k => new { id = k.To.UniqId, type = k.To.Type }, (k, v) =>
                {
                    Datastructures.Graphs.Graph.MergedEdge withChildren = Datastructures.Graphs.Graph.Edge.MergeSameEdges(v);
                    if (withChildren == null)
                        withChildren = new Datastructures.Graphs.Graph.MergedEdge(v.First());

                    return withChildren;
                })
                .OrderBy(m => m.To.PrintName())
                .ToArray();

            if (rels.Count() == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder(512);
            List<Datastructures.Graphs.Graph.Edge> deepRels = new List<Datastructures.Graphs.Graph.Edge>();
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
                if (withStats && rel.To.Type == Datastructures.Graphs.Graph.Node.NodeType.Company)
                    stat = Firmy.Get(rel.To.Id).StatistikaRegistruSmluv(); //new Analysis.SubjectStatistic(rel.To.Id);

                string subjId = rel.To.Type == Datastructures.Graphs.Graph.Node.NodeType.Company ? rel.To.Id : "Osoba";
                string subjName = rel.To.PrintName();
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
                        sb.Append(PrintFlatRelations(rel, level + 1, relations, typ, renderedIds, withStats));
                        break;
                    case Relation.TiskEnum.Html:
                        if (withStats && stat != null)
                            sb.AppendFormat("<li class='{3} {6}'><a href='/subjekt/{0}'>{0}:{1}</a>{7}; {4}, celkem {5}. {2}</li>",
                                subjId,
                                subjName,
                                PrintFlatRelations(rel, level + 1, relations, typ, renderedIds, withStats),
                                last ? "" : "connect",
                                Devmasters.Lang.CS.Plural.Get(stat.Summary().PocetSmluv, Util.Consts.csCulture, "{0} smlouva","{0} smlouvy","{0} smluv"),
                                Smlouva.NicePrice(stat.Summary().CelkovaHodnotaSmluv, html: true, shortFormat: true),
                                "aktualnost" + ((int)rel.Aktualnost).ToString(),
                                (rel.Aktualnost < Relation.AktualnostType.Aktualni) ? rel.Doba(format:"/{0}/") : string.Empty
                            );
                        else
                            sb.AppendFormat("<li class='{3} {4}'><a href='/subjekt/{0}'><span class=''>{0}:{1}</span></a>{5}.  {2}</li>",
                                subjId,
                                subjName,
                                PrintFlatRelations(rel, level + 1, relations, typ, renderedIds, withStats),
                                last ? "" : "connect",
                                "aktualnost" + ((int)rel.Aktualnost).ToString(),
                                (rel.Aktualnost < Relation.AktualnostType.Aktualni) ? rel.Doba(format: "/{0}/") : string.Empty,
                                (!string.IsNullOrEmpty(highlightSubjId) && subjId == highlightSubjId) ? "highlighted" : ""
                            );

                        break;
                    case Relation.TiskEnum.Checkbox:
                        sb.AppendFormat(@"<li class=""{0} {1}""><input type=""checkbox"" name=""ico"" value=""{2}"" /> <span><b>{2}</b> {3}</span>{4}</li>"
                        , (last ? "" : "connect"),
                        ("aktualnost" + ((int)rel.Aktualnost).ToString()),
                        subjId, subjName,
                        PrintFlatRelations(rel, level + 1, relations, typ, renderedIds, withStats)
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
        
        public static string PrintName(this Datastructures.Graphs.Graph.Node node, bool html = false)
        {
            switch (node.Type)
            {
                case Datastructures.Graphs.Graph.Node.NodeType.Person:
                    return Osoby.GetById.Get(Convert.ToInt32(node.Id))?.FullNameWithYear(html) ?? "(neznámá osoba)";
                case Datastructures.Graphs.Graph.Node.NodeType.Company:
                default:
                    return Firmy.GetJmeno(node.Id);
            }
        }
        
        public static string ExportTabData(IEnumerable<Datastructures.Graphs.Graph.Edge> data)
        {
            if (data == null)
                return "";
            if (data.Count() == 0)
                return "";

            StringBuilder sb = new StringBuilder(1024);

            foreach (var i in data)
            {
                sb.AppendFormat($"{i.From?.Id}\t{i.From?.PrintName()}\t{i.To?.Id}\t{i.To?.PrintName()}\t{i.RelFrom?.ToShortDateString() ?? "Ø"}\t{i.RelTo?.ToShortDateString() ?? "Ø"}\t{i.Descr}\n");
            }
            return sb.ToString();
        }
        
        public static string ExportGraphJsonData(IEnumerable<Datastructures.Graphs.Graph.Edge> data)
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
                    nodes.Add(i.From.UniqId, new Graph.GraphJson(i.From, i.Distance - 1, i.Distance == 0));
                }
                if (i.To != null && !nodes.ContainsKey(i.To.UniqId))
                    nodes.Add(i.To.UniqId, new Graph.GraphJson(i.To, i.Distance));
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
    }
}