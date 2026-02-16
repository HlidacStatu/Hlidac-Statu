using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Cache;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static class FirmaVazbyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(FirmaVazbyRepo));

        public static async Task AddOrUpdateAsync(
            string vlastnikIco, string dcerinkaIco,
            int kod_angm, string funkce, decimal? share, DateTime? fromDate, DateTime? toDate
        )
        {
            await using (DbEntities db = new DbEntities())
            {
                var existing = await db.FirmaVazby
                    .FirstOrDefaultAsync(m => m.Ico == vlastnikIco
                                         && m.VazbakIco == dcerinkaIco
                                         && m.DatumOd == fromDate
                                         && m.DatumDo == toDate);
                
                if (existing == null)
                    existing = await db.FirmaVazby
                        .FirstOrDefaultAsync(m => m.Ico == vlastnikIco
                                             && m.VazbakIco == dcerinkaIco
                                             && m.DatumOd == fromDate);

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

                await db.SaveChangesAsync();
            }
        }

        public static async Task<string[]> IcosInHoldingAsync(string icoOfMother)
        {
            string[] res = Array.Empty<string>();

            Relation.AktualnostType aktualnost = Relation.AktualnostType.Nedavny;
            Firma f = await FirmaCache.GetAsync(icoOfMother);
            if (f != null && f?.Valid == true)
            {
                var aktualniVazby = await f.AktualniVazbyAsync(aktualnost);

                var icos = new string[] { f.ICO }
                    .Union(aktualniVazby.Select(s => s.To.Id))
                    .Distinct();

                var vazbyPresOsoby = aktualniVazby
                    .Where(o => o.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person)
                    .ToList();

                var icosPresLidiList = new List<string>();
                foreach (var vazba in vazbyPresOsoby)
                {
                    var osoba = await OsobaCache.GetPersonByIdAsync(Convert.ToInt32(vazba.To.Id));
                    if (osoba != null)
                    {
                        var osobaVazby = await osoba.AktualniVazbyAsync(Relation.CharakterVazbyEnum.VlastnictviKontrola, aktualnost);
                        icosPresLidiList.AddRange(osobaVazby.Select(v => v.To.Id));
                    }
                }

                var icosPresLidi = icosPresLidiList.Distinct();
                icos = icos.Union(icosPresLidi).Distinct();
                
                res = icos.ToArray();
            }
            return res;
        }
        private static async Task UpdateVazbyAsync(this Firma firma,
            Relation.CharakterVazbyEnum charakterVazby,
            bool refresh = false)
        {
            try
            {
                var _vazby = await Graph.VsechnyDcerineVazbyAsync(firma.ICO, charakterVazby, refresh);
                firma.SetVazbyForInstanceOnly(charakterVazby,_vazby);
            }
            catch (Exception)
            {
                firma.SetVazbyForInstanceOnly(charakterVazby, new HlidacStatu.DS.Graphs.Graph.Edge[] { });
            }
        }
        

        public static async Task<DS.Graphs.Graph.Edge[]> VazbyAsync(this Firma firma,
            Relation.CharakterVazbyEnum charakterVazby = Relation.CharakterVazbyEnum.VlastnictviKontrola,
            bool refresh = false)
        {
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
            {
                if (firma._vazby == null || refresh == true)
                {
                    await firma.UpdateVazbyAsync(charakterVazby, refresh);
                }
                return firma._vazby;
            }
            else
            {
                if (firma._vazbyUredni == null || refresh == true)
                {
                    await firma.UpdateVazbyAsync(charakterVazby, refresh);
                }

                return firma._vazbyUredni;
            }
        }

        public static void SetVazbyForInstanceOnly(this Firma firma,
            Relation.CharakterVazbyEnum charakterVazby,
            IEnumerable<HlidacStatu.DS.Graphs.Graph.Edge> value
            )
        {
            if (charakterVazby == Relation.CharakterVazbyEnum.VlastnictviKontrola)
                firma._vazby = value.ToArray();
            else if (charakterVazby == Relation.CharakterVazbyEnum.Uredni)
                firma._vazbyUredni = value.ToArray();
        }



        public static async Task<Firma[]> ParentFirmyAsync(this Firma firma, 
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost)
        {
            if (firma._parents == null)
            {
                var parents = new List<Firma>();
                foreach (var ico in _getAllParents(firma.ICO, charakterVazby, minAktualnost))
                {
                    var f = await FirmaCache.GetAsync(ico);
                    if (f != null && f.Valid == true)
                    {
                        parents.Add(f);
                    }
                }
                firma._parents = parents.ToArray();

            }

            return firma._parents;
        }

        public static HashSet<string> _getAllParents( string ico, 
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost,
            HashSet<string> currList = null)
        {
            currList = currList ?? new HashSet<string>();

            HlidacStatu.DS.Graphs.Graph.Edge[] _parentF = Graph.GetDirectParentRelationsFirmy(ico, charakterVazby).ToArray();
            var _parentVazby = _parentF
                .Where(m => m.Aktualnost >= minAktualnost);
                
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
                        var newParents = _getAllParents(f,charakterVazby, minAktualnost, currList);
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

        public static HlidacStatu.DS.Graphs.Graph.Edge[] ParentVazbaFirmy(this Firma firma, 
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost)
        {
            if (firma._parentVazbyFirmy == null)
                firma._parentVazbyFirmy = Graph.GetDirectParentRelationsFirmy(firma.ICO,charakterVazby).ToArray();
            return firma._parentVazbyFirmy.Where(m => m.Aktualnost >= minAktualnost).ToArray(); 
        }

        private static async Task<Firma[]> VsechnyDcerinnePodrizeneAsync(this Firma firma,
            Relation.CharakterVazbyEnum charakterVazby ,
            Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            var vazby = await firma.AktualniVazbyAsync(minAktualnost, charakterVazby, refresh);

            var grouped = vazby
                .Where(v => v.To != null && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .GroupBy(f => f.To.Id, v => v, (ico, v) => ico);

            var data = new List<Firma>();
            foreach (var ico in grouped)
            {
                data.Add(await FirmaCache.GetAsync(ico));
            }
            return data.ToArray();

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
        public static async Task<int> PocetPodrizenychSubjektuAsync(this Firma firma,
            Relation.CharakterVazbyEnum charakterVazby,
            Relation.AktualnostType minAktualnost, bool refresh = false)
        {
            //firma.UpdateVazbyFromDB(); //nemelo by tu byt.
            return (await firma.AktualniVazbyAsync(minAktualnost, charakterVazby,refresh))?
                .Select(m=>m.To.UniqId)?
                .Distinct()?
                .Count() ?? 0;
        }

        public static async Task<DS.Graphs.Graph.Edge[]> AktualniVazbyAsync(this Firma firma, 
            Relation.AktualnostType minAktualnost,
            Relation.CharakterVazbyEnum charakterVazby = Relation.CharakterVazbyEnum.VlastnictviKontrola,
            bool refresh = false)
        {
            //firma.UpdateVazbyFromDB(); //nemelo by tu byt.
            var vsechnyVazby = await firma.VazbyAsync(charakterVazby, refresh);
            return Relation.AktualniVazby(vsechnyVazby, minAktualnost, firma.VazbyRootEdge());
        }
        
        private static async Task InitializeGraphAsync(this Firma firma, Relation.CharakterVazbyEnum charakterVazby)
        {
            var uGraph = new UnweightedGraph();
            Vertex<string> _startingVertex = null;
      
            var vazbyVlastnictvi = await firma.VazbyAsync(charakterVazby);
            foreach (var vazba in vazbyVlastnictvi)
            {
                if (vazba.From is null)
                {
                    _startingVertex = new Vertex<string>(vazba.To.UniqId,vazba.To.UniqId);
                    continue;
                }

                if (vazba.To is null)
                    continue;

                var fromVertex = new Vertex<string>(vazba.From.UniqId,vazba.From.UniqId);
                var toVertex = new Vertex<string>(vazba.To.UniqId,vazba.To.UniqId);

                uGraph.AddEdge(fromVertex, toVertex, vazba.DateInterval(), vazba);
            }
            firma._setGraph(charakterVazby, uGraph);
            firma._setStartingVertext(charakterVazby, _startingVertex);
        }

        private static async Task<DS.Graphs.Graph.Edge[]> _vazbyProICOAsync(this Firma firma, Relation.CharakterVazbyEnum charakterVazby, string ico)
        {
            if (firma._getGraph(charakterVazby) is null || firma._getGraph(charakterVazby).Vertices.Count == 0)
                await firma.InitializeGraphAsync(charakterVazby);

            if (firma._getStartingVertext(charakterVazby) is null)
                firma._setStartingVertext(charakterVazby, new Vertex<string>(
                    HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + firma.ICO,
                    HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + firma.ICO
                    ));
            try
            {
                var graph = firma._getGraph(charakterVazby);
                var shortestPath = graph.ShortestPath(firma._getStartingVertext(charakterVazby), CreateVertexFromIco(ico));
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
            return new Vertex<string>($"{HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company}{ico}", $"{HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company}{ico}");
        }

        public static async Task UpdateVazbyFromDbAsync(this Firma firma, Relation.CharakterVazbyEnum charakterVazby = Relation.CharakterVazbyEnum.VlastnictviKontrola)
        {
            List<HlidacStatu.DS.Graphs.Graph.Edge> oldRel = new List<HlidacStatu.DS.Graphs.Graph.Edge>();

            var firstRel = await Graph.VsechnyDcerineVazbyAsync(firma.ICO, charakterVazby, true);

            firma.SetVazbyForInstanceOnly(charakterVazby, HlidacStatu.DS.Graphs.Graph.Edge.Merge(oldRel, firstRel).ToArray());
        }

        public static async Task FixVazbaDatumDoAsync()
        {
            await using DbEntities db = new DbEntities();
            await db.Database.ExecuteSqlRawAsync(@"update FirmaVazby
                   set DatumDo = fi.DatumZaniku 
                  from FirmaVazby fv
                  join Firma fi on fi.ICO = FV.ICO
                 where fv.datumdo is null
                   and fi.DatumZaniku is not null");
                
            await db.Database.ExecuteSqlRawAsync(@"update FirmaVazby
                   set DatumDo = fi.DatumZaniku 
                  from FirmaVazby fv
                  join Firma fi on fi.ICO = FV.VazbakICO
                 where fv.datumdo is null
                   and fi.DatumZaniku is not null");
        }
        
        private static IFusionCache MemoryCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(FirmaVazbyRepo));
        
        public static async Task<HlidacStatu.DS.Graphs.Graph.Edge[]> VazbyProIcoCachedAsync(Firma f, 
            Relation.CharakterVazbyEnum charakterVazby, string ico, bool refresh = false)
        {
            if (f is null)
            {
                return [];
            }
            string key = $"_VazbyFirmaProIcoCache:{f.ICO}_{charakterVazby}->{ico}";
            if (refresh)
            {
                await MemoryCache.RemoveAsync(key);
            }

            return await MemoryCache.GetOrSetAsync(key,
                _ => f._vazbyProICOAsync(charakterVazby, ico),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(2)));
        }
    }
}