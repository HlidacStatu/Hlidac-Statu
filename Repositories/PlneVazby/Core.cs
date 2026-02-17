using Devmasters.DT;
using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.PlneVazby
{
    public  class Core
    {
        public static async Task<List<AllPathsFinder.PathResult>> AllPathsAsync(Firma f,
    Relation.CharakterVazbyEnum charakterVazby, string ico,
    DateInterval searchInterval = null,
    bool refresh = false)
        {
            var vazby = await f.VazbyAsync(Relation.CharakterVazbyEnum.VlastnictviKontrola, refresh);
            var edges = vazby.Select(edge => new HSEdge2(
                new Vertex2<string>(edge.From?.UniqId ?? HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + ico, edge.From?.UniqId ?? HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + ico),
                new Vertex2<string>(edge.To.UniqId, edge.To.UniqId),
                edge.DateInterval(), edge)).ToList();

            List<AllPathsFinder.PathResult> paths = AllPathsFinder.FindAllPaths(
                edges,
                HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + f.ICO,
                HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + ico,
                searchInterval
                );
            return paths;
        }

        public static async Task<List<AllPathsFinder.PathResult>> AllPathsAsync(Osoba o,
Relation.CharakterVazbyEnum charakterVazby, string ico,
DateInterval searchInterval = null,
bool refresh = false)
        {
            var vazby = await o.VazbyAsync(Relation.CharakterVazbyEnum.VlastnictviKontrola, refresh);
            var edges = vazby.Select(edge => new HSEdge2(
                new Vertex2<string>(
                    edge.From?.UniqId ?? HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Person + o.InternalId, 
                    edge.From?.UniqId ?? HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Person + o.InternalId),
                new Vertex2<string>(edge.To.UniqId, edge.To.UniqId),
                edge.DateInterval(), edge)).ToList();
            
            List<AllPathsFinder.PathResult> paths = AllPathsFinder.FindAllPaths(
                edges,
                HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Person + o.InternalId,
                HlidacStatu.DS.Graphs.Graph.Node.Prefix_NodeType_Company + ico,
                searchInterval
                );
            return paths;
        }

    }
}
