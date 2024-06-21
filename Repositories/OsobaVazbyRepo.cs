using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using HlidacStatu.Entities;


using System;
using System.Linq;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static class OsobaVazbyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobaVazbyRepo));
        
        public static void AddOrUpdate(
            int osobaId, int vazbakOsobaId,
            int kod_angm, string funkce, decimal? share, 
            DateTime? fromDate, DateTime? toDate,
            DateTime? zapisORfromDate, DateTime? zapisORtoDate,
            string zdroj = ""
        )
        {
            using (DbEntities db = new DbEntities())
            {
                var existing = db.OsobaVazby.AsQueryable()
                    .Where(m =>
                        m.OsobaId == osobaId
                        && m.VazbakOsobaId == vazbakOsobaId
                        && m.DatumOd == fromDate
                        && m.DatumDo == toDate
                    )
                    .FirstOrDefault();
                if (existing == null)
                    existing = db.OsobaVazby.AsQueryable()
                        .Where(m =>
                            m.OsobaId == osobaId
                            && m.VazbakOsobaId == vazbakOsobaId
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
                    if (existing.DatumOd != fromDate)
                        existing.DatumOd = fromDate;
                    if (existing.DatumDo != toDate)
                        existing.DatumDo = toDate;
                    existing.LastUpdate = DateTime.Now;
                }
                else //new
                {
                    OsobaVazby af = new OsobaVazby();
                    af.OsobaId = osobaId;
                    af.VazbakIco = "";
                    af.VazbakOsobaId = vazbakOsobaId;
                    af.DatumOd = fromDate;
                    af.DatumDo = toDate;
                    af.ZapisOROd = zapisORfromDate;
                    af.ZapisORDo = zapisORtoDate;
                    af.TypVazby = kod_angm;
                    af.PojmenovaniVazby = funkce;
                    af.Podil = share;
                    af.LastUpdate = DateTime.Now;
                    db.OsobaVazby.Add(af);
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "");
                    throw;
                }
            }
        }

        public static void AddOrUpdate(
            int osobaId, string dcerinkaIco,
            int kod_angm, string funkce, decimal? share, 
            DateTime? fromDate, DateTime? toDate,
            DateTime? zapisORfromDate, DateTime? zapisORtoDate,
            string zdroj = ""
        )
        {
            using (DbEntities db = new DbEntities())
            {
                var existing = db.OsobaVazby.AsQueryable()
                    .Where(m =>
                        m.OsobaId == osobaId
                        && m.VazbakIco == dcerinkaIco
                        && m.DatumOd == fromDate
                        && m.DatumDo == toDate
                    )
                    .FirstOrDefault();
                if (existing == null)
                    existing = db.OsobaVazby.AsQueryable()
                        .Where(m =>
                            m.OsobaId == osobaId
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
                    if (existing.DatumOd != fromDate)
                        existing.DatumOd = fromDate;
                    if (existing.DatumDo != toDate)
                        existing.DatumDo = toDate;
                    existing.LastUpdate = DateTime.Now;
                }
                else //new
                {
                    OsobaVazby af = new OsobaVazby();
                    af.OsobaId = osobaId;
                    af.VazbakIco = dcerinkaIco;
                    af.DatumOd = fromDate;
                    af.DatumDo = toDate;
                    af.ZapisOROd = zapisORfromDate;
                    af.ZapisORDo = zapisORtoDate;
                    af.TypVazby = kod_angm;
                    af.PojmenovaniVazby = funkce;
                    af.Podil = share;
                    af.LastUpdate = DateTime.Now;
                    db.OsobaVazby.Add(af);
                }

                db.SaveChanges();
            }
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge VazbyRootEdge(this Osoba osoba)
        {
            return new HlidacStatu.DS.Graphs.Graph.Edge()
            {
                From = null,
                Root = true,
                To = new HlidacStatu.DS.Graphs.Graph.Node() { Id = osoba.InternalId.ToString(), Type = HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person },
                RelFrom = null,
                RelTo = null,
                Distance = 0,
                Aktualnost = Relation.AktualnostType.Aktualni
            };

        }
        public static HlidacStatu.DS.Graphs.Graph.Edge[] VazbyProICO(this Osoba osoba, string ico)
        {
            return _vazbyProIcoCache.Get((osoba, ico));
        }

        private static HlidacStatu.DS.Graphs.Graph.Edge[] _vazbyProICO(this Osoba osoba, string ico)
        {
            if (osoba._graph is null || osoba._graph.Vertices.Count == 0)
                osoba.InitializeGraph();

            if (osoba._startingVertex is null)
                osoba._startingVertex = new Vertex<string>(HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Person + osoba.InternalId);

            try
            {
                var shortestPath = osoba._graph.ShortestPath(osoba._startingVertex, CreateVertexFromIco(ico));
                if (shortestPath == null)
                    return Array.Empty<HlidacStatu.DS.Graphs.Graph.Edge>();

                var result = shortestPath.Select(x => ((Edge<HlidacStatu.DS.Graphs.Graph.Edge>)x).BindingPayload).ToArray();
                return result; // shortestGraph.ShortestTo(ico).ToArray();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Vazby ERROR for " + osoba.NameId);
                return Array.Empty<HlidacStatu.DS.Graphs.Graph.Edge>();
            }
        }

        private static void InitializeGraph(this Osoba osoba)
        {
            osoba._graph = new UnweightedGraph();
            foreach (var vazba in osoba.Vazby())
            {
                if (vazba.From is null)
                {
                    osoba._startingVertex = new Vertex<string>(vazba.To.UniqId);
                    continue;
                }

                if (vazba.To is null)
                    continue;

                var fromVertex = new Vertex<string>(vazba.From.UniqId);
                var toVertex = new Vertex<string>(vazba.To.UniqId);

                osoba._graph.AddEdge(fromVertex, toVertex, vazba);
            }
        }

        private static Vertex<string> CreateVertexFromIco(string ico)
        {
            return new Vertex<string>($"{HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company}{ico}");
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge[] Vazby(this Osoba osoba, bool refresh = false)
        {
            if (refresh || osoba._vazby == null)
            {
                osoba.updateVazby(refresh);
            }

            return osoba._vazby;
        }

        public static int PocetPodrizenychSubjektu(this Osoba osoba, Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            //firma.UpdateVazbyFromDB(); //nemelo by tu byt.
            return osoba.AktualniVazby(minAktualnost, refresh)?
                .Select(m => m.To.UniqId)?
                .Distinct()?
                .Count() ?? 0;
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge[] AktualniVazby(this Osoba osoba, Relation.AktualnostType minAktualnost, bool refresh=false)
        {
            return Relation.AktualniVazby(osoba.Vazby(refresh), minAktualnost, osoba.VazbyRootEdge());
        }

        private static void updateVazby(this Osoba osoba, bool refresh = false)
        {
            try
            {
                osoba._vazby = Graph.VsechnyDcerineVazby(osoba, refresh)
                    .ToArray();
            }
            catch (Exception)
            {
                osoba._vazby = new HlidacStatu.DS.Graphs.Graph.Edge[] { };
            }
        }

        static private Devmasters.Cache.LocalMemory.Manager<HlidacStatu.DS.Graphs.Graph.Edge[], (Osoba o, string ico)> _vazbyProIcoCache
            = Devmasters.Cache.LocalMemory.Manager<HlidacStatu.DS.Graphs.Graph.Edge[], (Osoba o, string ico)>
                .GetSafeInstance("_vazbyOsobaProIcoCache", f => { return f.o._vazbyProICO(f.ico); },
                    TimeSpan.FromHours(2), k => (k.o.NameId + "-" + k.ico)
                );
    }
}