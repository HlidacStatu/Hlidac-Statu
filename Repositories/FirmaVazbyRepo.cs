using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Util;
using HlidacStatu.Util.Cache;
using HlidacStatu.Datastructures.Graphs2;

namespace HlidacStatu.Repositories
{
    public static class FirmaVazbyRepo
    {
        public static void AddOrUpdate(
            string vlastnikIco, string dcerinkaIco,
            int kod_angm, string funkce, decimal? share, DateTime? fromDate, DateTime? toDate
        )
        {
            using (DbEntities db = new DbEntities())
            {
                var existing = db.FirmaVazby.AsQueryable()
                    .Where(m =>
                        m.Ico == vlastnikIco
                        && m.VazbakIco == dcerinkaIco
                        && m.DatumOd == fromDate
                        && m.DatumDo == toDate
                    )
                    .FirstOrDefault();
                if (existing == null)
                    existing = db.FirmaVazby.AsQueryable()
                        .Where(m =>
                            m.Ico == vlastnikIco
                            && m.VazbakIco == dcerinkaIco
                            && m.DatumOd == fromDate
                        )
                        .FirstOrDefault();

                if (existing != null)
                {
                    //update
                    existing.TypVazby = kod_angm;
                    existing.PojmenovaniVazby = funkce;
                    if (existing.Podil != share)
                        existing.Podil = share;
                    if (existing.DatumOd != fromDate && fromDate.HasValue)
                        existing.DatumOd = fromDate;
                    if (existing.DatumDo != toDate && toDate.HasValue)
                        existing.DatumDo = toDate;
                    existing.LastUpdate = DateTime.Now;
                }
                else //new
                {
                    FirmaVazby af = new FirmaVazby();
                    af.Ico = vlastnikIco;
                    af.VazbakIco = dcerinkaIco;
                    af.DatumOd = fromDate;
                    af.DatumDo = toDate;
                    af.TypVazby = kod_angm;
                    af.PojmenovaniVazby = funkce;
                    af.Podil = share;
                    af.LastUpdate = DateTime.Now;
                    db.FirmaVazby.Add(af);
                }

                db.SaveChanges();
            }
        }

        private static void updateVazby(this Firma firma, bool refresh = false)
        {
            try
            {
                firma._vazby = Graph.VsechnyDcerineVazby(firma.ICO, refresh)
                    .ToArray();
            }
            catch (Exception)
            {
                firma._vazby = new Datastructures.Graphs.Graph.Edge[] { };
            }
        }

        

        public static Datastructures.Graphs.Graph.Edge[] Vazby(this Firma firma, bool refresh = false)
        {
            if (firma._vazby == null || refresh == true)
            {
                firma.updateVazby(refresh);
            }

            return firma._vazby;
        }

        public static void Vazby(this Firma firma, IEnumerable<Datastructures.Graphs.Graph.Edge> value)
        {
            firma._vazby = value.ToArray();
        }

        

        public static Firma[] ParentVazbyFirmy(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            if (firma._parents == null)
            {
                firma._parents = firma._getAllParents(firma.ICO, minAktualnost).ToArray();
            }

            return firma._parents;
        }

        public static List<Firma> _getAllParents(this Firma firma, string ico, Relation.AktualnostType minAktualnost,
            List<Firma> currList = null)
        {
            currList = currList ?? new List<Firma>();

            var _parentVazbaFirma = Relation
                .AktualniVazby(Graph.GetDirectParentRelationsFirmy(ico).ToArray(), minAktualnost)
                .Select(m => Firmy.Get(m.From.Id))
                .Where(m => m != null)
                .ToArray();
            if (_parentVazbaFirma.Count() > 0)
            {
                bool addedNew = false;
                foreach (var f in _parentVazbaFirma)
                {
                    if (currList.Any(m => m.ICO == f.ICO))
                    {
                        //skip
                    }
                    else
                    {
                        currList.Insert(0, f);
                        addedNew = true;
                    }
                }
                if (addedNew)
                    return firma._getAllParents(currList[0].ICO, minAktualnost, currList);
                else
                    return currList;
            }
            else
                return currList;
        }

        public static Datastructures.Graphs.Graph.Edge[] ParentVazbaFirmy(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            if (firma._parentVazbyFirmy == null)
                firma._parentVazbyFirmy = Graph.GetDirectParentRelationsFirmy(firma.ICO).ToArray();
            return Relation.AktualniVazby(firma._parentVazbyFirmy, minAktualnost);
        }

        

        public static Datastructures.Graphs.Graph.Edge[] ParentVazbyOsoby(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            if (firma._parentVazbyOsoby == null)
                firma._parentVazbyOsoby = Graph.GetDirectParentRelationsOsoby(firma.ICO).ToArray();
            return Relation.AktualniVazby(firma._parentVazbyOsoby, minAktualnost);
        }

        public static Datastructures.Graphs.Graph.Edge[] AktualniVazby(this Firma firma, Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            firma.UpdateVazbyFromDB();
            return Relation.AktualniVazby(firma.Vazby(refresh), minAktualnost);
        }


        public static Datastructures.Graphs.Graph.Edge[] VazbyProICO(this Firma firma, string ico)
        {
            return _vazbyProIcoCache.Get((firma, ico));
        }

        static private MemoryCacheManager<Datastructures.Graphs.Graph.Edge[], (Firma f, string ico)> _vazbyProIcoCache
            = MemoryCacheManager<Datastructures.Graphs.Graph.Edge[], (Firma f, string ico)>
                .GetSafeInstance("_vazbyFirmaProIcoCache", f => { return f.f._vazbyProICO(f.ico); },
                    TimeSpan.FromHours(2), k => (k.f.ICO + "-" + k.ico)
                );


        

        private static void InitializeGraph(this Firma firma)
        {
            firma._graph = new UnweightedGraph();
            foreach (var vazba in firma.Vazby())
            {
                if (vazba.From is null)
                {
                    firma._startingVertex = new Vertex<string>(vazba.To.UniqId);
                    continue;
                }

                if (vazba.To is null)
                    continue;

                var fromVertex = new Vertex<string>(vazba.From.UniqId);
                var toVertex = new Vertex<string>(vazba.To.UniqId);

                firma._graph.AddEdge(fromVertex, toVertex, vazba);
            }
        }

        private static Datastructures.Graphs.Graph.Edge[] _vazbyProICO(this Firma firma, string ico)
        {
            if (firma._graph is null || firma._graph.Vertices.Count == 0)
                firma.InitializeGraph();

            if (firma._startingVertex is null)
                firma._startingVertex = new Vertex<string>(HlidacStatu.Datastructures.Graphs.Graph.Node.Prefix_NodeType_Company + firma.ICO);

            try
            {
                var shortestPath = firma._graph.ShortestPath(firma._startingVertex, CreateVertexFromIco(ico));
                if (shortestPath == null)
                    return Array.Empty<Datastructures.Graphs.Graph.Edge>();
                var result = shortestPath.Select(x => ((Edge<Datastructures.Graphs.Graph.Edge>) x).BindingPayload).ToArray();
                return result; // shortestGraph.ShortestTo(ico).ToArray();
            }
            catch (Exception e)
            {
                Consts.Logger.Error("Vazby ERROR for " + firma.ICO, e);
                return Array.Empty<Datastructures.Graphs.Graph.Edge>();
            }
        }

        private static Vertex<string> CreateVertexFromIco(string ico)
        {
            return new Vertex<string>($"{HlidacStatu.Datastructures.Graphs.Graph.Node.Prefix_NodeType_Company}{ico}");
        }
        
        public static void UpdateVazbyFromDB(this Firma firma)
        {
            List<Datastructures.Graphs.Graph.Edge> oldRel = new List<Datastructures.Graphs.Graph.Edge>();
            
            var firstRel = Graph.VsechnyDcerineVazby(firma.ICO, true);

            firma.Vazby(Datastructures.Graphs.Graph.Edge.Merge(oldRel, firstRel).ToArray());
        }
    }
}