using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HlidacStatu.Searching;

namespace HlidacStatu.Repositories
{
    public static class FirmaVazbyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(FirmaVazbyRepo));
        
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

        public static string[] IcosInHolding(string icoOfMother)
        {
            string[] res = Array.Empty<string>();

            Relation.AktualnostType aktualnost = Relation.AktualnostType.Nedavny;
            Firma f = Firmy.Get(icoOfMother);
            if (f != null && f.Valid)
            {
                var icos = new string[] { f.ICO }
                    .Union(
                        f.AktualniVazby(aktualnost)
                        .Select(s => s.To.Id)
                    )
                    .Distinct();
                var icosPresLidi = f.AktualniVazby(aktualnost)
                        .Where(o => o.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person)
                        .Select(o => Osoby.GetById.Get(Convert.ToInt32(o.To.Id)))
                        .Where(o => o != null)
                        .SelectMany(o => o.AktualniVazby(aktualnost))
                        .Select(v => v.To.Id)
                        .Distinct();
                icos = icos.Union(icosPresLidi).Distinct();

                res = icos.ToArray();
            }
            return res;
        }
        private static void UpdateVazby(this Firma firma, bool refresh = false)
        {
            try
            {
                firma._vazby = Graph.VsechnyDcerineVazby(firma.ICO, refresh)
                    .ToArray();
            }
            catch (Exception)
            {
                firma._vazby = new HlidacStatu.DS.Graphs.Graph.Edge[] { };
            }
        }



        public static DS.Graphs.Graph.Edge[] Vazby(this Firma firma, bool refresh = false)
        {
            if (firma._vazby == null || refresh == true)
            {
                firma.UpdateVazby(refresh);
            }

            return firma._vazby;
        }

        public static void Vazby(this Firma firma, IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> value)
        {
            firma._vazby = value.ToArray();
        }



        public static Firma[] ParentFirmy(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            if (firma._parents == null)
            {
                firma._parents = _getAllParents(firma.ICO, minAktualnost)
                    .Select(m=> Firmy.Get(m))
                    .Where(m=> m!=null || m?.Valid == true)
                    .ToArray();
            }

            return firma._parents;
        }

        public static HashSet<string> _getAllParents( string ico, Relation.AktualnostType minAktualnost,
            HashSet<string> currList = null)
        {
            currList = currList ?? new HashSet<string>();

            HlidacStatu.DS.Graphs.Graph.Edge[] _parentF = Graph.GetDirectParentRelationsFirmy(ico).ToArray();
            var _parentVazby = _parentF
                .Where(m => m.Aktualnost >= minAktualnost)
                //.Where(m => Devmasters.DT.Util.IsOverlappingIntervals(parentF.RelFrom, parent.RelTo, m.RelFrom, m.RelTo))
            ;

            //List<HlidacStatu.DS.Graphs.Graph.Edge> _parentVazby = new List<HlidacStatu.DS.Graphs.Graph.Edge>();
            //foreach (var p in _parentF)
            //{
            //    if (Util.DataValidators.CheckCZICO(p.From.Id))
            //    {
            //        var parentFirma = Firmy.Get(p.From.Id);
            //        _parentVazby.AddRange(Relation.AktualniVazby(new Edge[] { p }, minAktualnost, parentFirma.VazbyRootEdge()));
            //    }
            //}


            var _parentVazbyIco = _parentVazby
                .Select(m => m.From.Id)
                .Where(m => Util.DataValidators.CheckCZICO(m))
                .ToArray();
            if (_parentVazbyIco.Count() > 0)
            {
                foreach (var f in _parentVazbyIco)
                {
                    if (currList.Contains(f))
                    {
                        //skip
                    }
                    else
                    {
                        currList.Add(f);
                        var newParents = _getAllParents(f, minAktualnost, currList);
                        foreach (var np in newParents)
                        {
                            currList.Add(np);
                        }
                    }
                }
                return currList;
            }
            else
                return currList;
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge[] ParentVazbaFirmy(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            if (firma._parentVazbyFirmy == null)
                firma._parentVazbyFirmy = Graph.GetDirectParentRelationsFirmy(firma.ICO).ToArray();
            return firma._parentVazbyFirmy.Where(m => m.Aktualnost >= minAktualnost).ToArray(); 
        }



        public static HlidacStatu.DS.Graphs.Graph.Edge[] ParentVazbyOsoby(this Firma firma, Relation.AktualnostType minAktualnost)
        {
            throw new NotImplementedException(); //funguje blbe, dobre funguje firma.Osoby_v_OR
            if (firma._parentVazbyOsoby == null)
                firma._parentVazbyOsoby = Graph.GetDirectParentRelationsOsoby(firma.ICO).ToArray();
            return Relation.AktualniVazby(firma._parentVazbyOsoby, minAktualnost, firma.VazbyRootEdge());
        }

        private static Firma[] _vsechnyDcerinnePodrizene(this Firma firma, Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            var vazby = firma.AktualniVazby(minAktualnost, refresh);

            var data = vazby
                .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                        .GroupBy(f => f.To.Id, v => v, (ico, v) => new
                        {
                            ICO = ico,
                            Firma = Firmy.Get(ico)
                        });
            return data.Select(m => m.Firma).ToArray();
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge VazbyRootEdge(this Firma firma)
        {
            return new HlidacStatu.DS.Graphs.Graph.Edge()
            {
                From = null,
                Root = true,
                To = new HlidacStatu.DS.Graphs.Graph.Node() { Id = firma.ICO, Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company },
                RelFrom = null,
                RelTo = null,
                Distance = 0,
                Aktualnost = Relation.AktualnostType.Aktualni
            };

        }
        public static int PocetPodrizenychSubjektu(this Firma firma, Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            //firma.UpdateVazbyFromDB(); //nemelo by tu byt.
            return firma.AktualniVazby(minAktualnost,refresh)?
                .Select(m=>m.To.UniqId)?
                .Distinct()?
                .Count() ?? 0;
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge[] AktualniVazby(this Firma firma, Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            //firma.UpdateVazbyFromDB(); //nemelo by tu byt.
            var vsechnyVazby = firma.Vazby(refresh);
            return Relation.AktualniVazby(vsechnyVazby, minAktualnost, firma.VazbyRootEdge());
        }


        public static HlidacStatu.DS.Graphs.Graph.Edge[] VazbyProICO(this Firma firma, string ico)
        {
            return _vazbyProIcoCache.Get((firma, ico));
        }

        static private Devmasters.Cache.LocalMemory.Manager<HlidacStatu.DS.Graphs.Graph.Edge[], (Firma f, string ico)> _vazbyProIcoCache
            = Devmasters.Cache.LocalMemory.Manager<HlidacStatu.DS.Graphs.Graph.Edge[], (Firma f, string ico)>
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

        private static HlidacStatu.DS.Graphs.Graph.Edge[] _vazbyProICO(this Firma firma, string ico)
        {
            if (firma._graph is null || firma._graph.Vertices.Count == 0)
                firma.InitializeGraph();

            if (firma._startingVertex is null)
                firma._startingVertex = new Vertex<string>(HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + firma.ICO);

            try
            {
                var shortestPath = firma._graph.ShortestPath(firma._startingVertex, CreateVertexFromIco(ico));
                if (shortestPath == null)
                    return Array.Empty<HlidacStatu.DS.Graphs.Graph.Edge>();
                var result = shortestPath.Select(x => ((Edge<HlidacStatu.DS.Graphs.Graph.Edge>)x).BindingPayload).ToArray();
                return result; // shortestGraph.ShortestTo(ico).ToArray();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Vazby ERROR for " + firma.ICO);
                return Array.Empty<HlidacStatu.DS.Graphs.Graph.Edge>();
            }
        }

        private static Vertex<string> CreateVertexFromIco(string ico)
        {
            return new Vertex<string>($"{HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company}{ico}");
        }

        public static void UpdateVazbyFromDb(this Firma firma)
        {
            List<HlidacStatu.DS.Graphs.Graph.Edge> oldRel = new List<HlidacStatu.DS.Graphs.Graph.Edge>();

            var firstRel = Graph.VsechnyDcerineVazby(firma.ICO, true);

            firma.Vazby(HlidacStatu.DS.Graphs.Graph.Edge.Merge(oldRel, firstRel).ToArray());
        }

        public static void FixVazbaDatumDo()
        {
            using (DbEntities db = new DbEntities())
            {
                db.Database.ExecuteSqlRaw(@"update FirmaVazby
                   set DatumDo = fi.DatumZaniku 
                  from FirmaVazby fv
                  join Firma fi on fi.ICO = FV.ICO
                 where fv.datumdo is null
                   and fi.DatumZaniku is not null");
                
                db.Database.ExecuteSqlRaw(@"update FirmaVazby
                   set DatumDo = fi.DatumZaniku 
                  from FirmaVazby fv
                  join Firma fi on fi.ICO = FV.VazbakICO
                 where fv.datumdo is null
                   and fi.DatumZaniku is not null");
            }
        }
    }
}