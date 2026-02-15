using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using HlidacStatu.Entities;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static class OsobaVazbyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobaVazbyRepo));

        public static async Task<string[]> Icos_s_VazbouNaOsobuAsync(string nameId, Relation.CharakterVazbyEnum charakterVazby = Relation.CharakterVazbyEnum.VlastnictviKontrola)
        {
            string[] res = Array.Empty<string>();
            Osoba p = await OsobaCache.GetPersonByNameIdAsync(nameId);

            if (p != null)
            {
                var icos = (await p.AktualniVazbyAsync(charakterVazby, Relation.AktualnostType.Nedavny))
                            .Where(w => !string.IsNullOrEmpty(w.To.Id))
                            .Select(w => w.To.Id)
                            .Distinct().ToArray();


                if (icos != null && icos.Length > 0)
                {
                    res = icos.ToArray();
                }
            }
            return res;
        }

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
                var existing = db.OsobaVazby
                    .AsQueryable()
                    .FirstOrDefault(m => m.OsobaId == osobaId
                                         && m.VazbakOsobaId == vazbakOsobaId
                                         && m.DatumOd == fromDate
                                         && m.DatumDo == toDate);
                if (existing == null)
                    existing = db.OsobaVazby
                        .AsQueryable()
                        .FirstOrDefault(m => m.OsobaId == osobaId
                                             && m.VazbakOsobaId == vazbakOsobaId
                                             && m.DatumOd == fromDate);

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
                var existing = db.OsobaVazby
                    .AsQueryable()
                    .FirstOrDefault(m => m.OsobaId == osobaId
                                         && m.VazbakIco == dcerinkaIco
                                         && m.DatumOd == fromDate
                                         && m.DatumDo == toDate);
                if (existing == null)
                    existing = db.OsobaVazby
                        .AsQueryable()
                        .FirstOrDefault(m => m.OsobaId == osobaId
                                             && m.VazbakIco == dcerinkaIco
                                             && m.DatumOd == fromDate);

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

        private static async Task<DS.Graphs.Graph.Edge[]> _vazbyProICOAsync(this Osoba osoba, string ico, Relation.CharakterVazbyEnum charakterVazby)
        {
            if (osoba._graph is null || osoba._graph.Vertices.Count == 0)
                await osoba.InitializeGraphAsync(charakterVazby);

            osoba._startingVertex ??=
                new Vertex<string>(HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Person + osoba.InternalId);

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

        private static async Task InitializeGraphAsync(this Osoba osoba, Relation.CharakterVazbyEnum charakterVazby)
        {
            osoba._graph = new UnweightedGraph();
            var vazbyVlastnictvi = await osoba.VazbyAsync(charakterVazby);
            foreach (var vazba in vazbyVlastnictvi)
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

        public static async Task<DS.Graphs.Graph.Edge[]> VazbyAsync(this Osoba osoba, Relation.CharakterVazbyEnum charakterVazby, bool refresh = false)
        {
            if (osoba == null)
                return [];

            if (refresh || osoba._vazby == null || osoba._vazbyUredni == null)
            {
                await osoba.UpdateVazbyForInstanceAsync(charakterVazby, refresh);
            }

            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
                return osoba._vazby;
            else 
                return osoba._vazbyUredni;
        }

        public static async Task<int> PocetPodrizenychSubjektuAsync(this Osoba osoba,
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            return (await osoba.AktualniVazbyAsync(charakterVazby, minAktualnost, refresh))?
                .Select(m => m.To.UniqId)?
                .Distinct()?
                .Count() ?? 0;
        }
        public static async Task<DS.Graphs.Graph.Edge[]> PrimaAngazovanostAsync(this Osoba osoba,
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            var vazby = Relation.AktualniVazby(await osoba.VazbyAsync(charakterVazby, refresh), minAktualnost, osoba.VazbyRootEdge());

            var res = vazby.Where(v => v.Distance == 1).ToArray();
            return res;
        }

        public static async Task<DS.Graphs.Graph.Edge[]> AktualniVazbyAsync(this Osoba osoba,
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            return Relation.AktualniVazby(await osoba.VazbyAsync(charakterVazby, refresh), minAktualnost, osoba.VazbyRootEdge());
        }

        private static async Task UpdateVazbyForInstanceAsync(this Osoba osoba, Relation.CharakterVazbyEnum charakterVazby,
            bool refresh = false)
        {
            if (osoba == null)
                return;

            try
            {
                if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
                    osoba._vazby = (await Graph.VsechnyDcerineVazbyAsync(osoba, charakterVazby, refresh)).ToArray();
                else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
                    osoba._vazbyUredni = (await Graph.VsechnyDcerineVazbyAsync(osoba, charakterVazby, refresh)).ToArray();
            }
            catch (Exception)
            {
                osoba._vazby = [];
                osoba._vazbyUredni = [];
            }
        }

        private static IFusionCache MemoryCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(OsobaVazbyRepo));

        public static async Task<HlidacStatu.DS.Graphs.Graph.Edge[]> VazbyProIcoCachedAsync(Osoba osoba,
            Relation.CharakterVazbyEnum charakterVazby, 
            string ico, bool refresh = false)
        {
            if (osoba is null || string.IsNullOrWhiteSpace(ico))
            {
                return [];
            }
            string key = $"_VazbyOsobaProIcoCache:{osoba.NameId}_{ico}_{charakterVazby.ToString()}";
            if (refresh)
            {
                await MemoryCache.RemoveAsync(key);
            }

            return await MemoryCache.GetOrSetAsync(key,
                _ => osoba._vazbyProICOAsync(ico, charakterVazby),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(2)));
        }
    }
}