using Devmasters.DT;
using HlidacStatu.DS.Graphs2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HlidacStatu.DS.Graphs.Graph;

namespace HlidacStatu.Repositories.PlneVazby
{
    public static class AllPathsFinder
    {
        public class PathResult
        {
            public List<HSEdge2> Edges { get; set; } = new();
            public DateInterval ValidInterval { get; set; }

            public override string ToString()
            {
                var route = string.Join(" → ", Edges.Select(e => e.From.Id))
                            + " → " + Edges.Last().To.Id;
                var interval = ValidInterval?.ToNiceString("–", "∞", "yyyy-MM-dd") ?? "N/A";
                return $"{route}  (platnost: {interval})";
            }
        }

        /// <summary>
        /// Najde VŠECHNY cesty (bez cyklů) mezi dvěma firmami,
        /// jejichž průnik časových intervalů s hledaným intervalem je neprázdný.
        /// Výsledky seřazeny od nejkratší cesty.
        /// </summary>
        public static List<PathResult> FindAllPaths(
            IReadOnlyList<HSEdge2> edges,
            string startId,
            string endId,
            DateInterval searchInterval = null)
        {
            // null = neomezený interval
            var interval = searchInterval ?? new DateInterval((DateTime?)null, (DateTime?)null);

            var results = new List<PathResult>();

            if (startId == endId)
            {
                results.Add(new PathResult { ValidInterval = interval });
                return results;
            }

            var adjacency = edges
                .GroupBy(e => e.From.Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            // DFS s backtrackingem
            var pathNodes = new HashSet<string> { startId };
            var currentPath = new List<HSEdge2>();

            Dfs(startId, endId, interval, adjacency, pathNodes, currentPath, results);

            // Seřadit od nejkratší cesty
            results.Sort((a, b) => a.Edges.Count.CompareTo(b.Edges.Count));

            var deduped = RemoveDuplicates(results);
            return deduped;
        }

        public static List<PathResult> RemoveDuplicates(IEnumerable<PathResult> paths)
        {
            if (paths == null)
                return null;

            var pathList = paths.ToList();
            if (pathList.Count <= 1)
                return pathList;

            // Group paths by their route (sequence of node IDs)
            var groups = pathList.GroupBy(p => GetRouteKey(p));

            var results = new List<PathResult>();
            foreach (var group in groups)
            {
                var groupPaths = group.ToList();

                // Merge all intervals for this route
                var intervals = groupPaths.Select(p => p.ValidInterval).ToArray();
                var merged = DateInterval.MergeDateIntervals(intervals);

                // Create one PathResult per merged interval, reusing edges from the first path in the group
                var templateEdges = groupPaths[0].Edges;
                foreach (var interval in merged)
                {
                    results.Add(new PathResult
                    {
                        Edges = new List<HSEdge2>(templateEdges),
                        ValidInterval = interval
                    });
                }
            }

            results.Sort((a, b) => a.Edges.Count.CompareTo(b.Edges.Count));
            return results;

        }


        private static string GetRouteKey(PathResult path)
        {
            if (path.Edges == null || path.Edges.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var edge in path.Edges)
            {
                sb.Append(edge.From.Id);
                sb.Append('>');
            }
            sb.Append(path.Edges[path.Edges.Count - 1].To.Id);
            return sb.ToString();
        }

        private static void Dfs(
            string current,
            string endId,
            DateInterval currentInterval,
            Dictionary<string, List<HSEdge2>> adjacency,
            HashSet<string> pathNodes,
            List<HSEdge2> currentPath,
            List<PathResult> results)
        {
            if (!adjacency.TryGetValue(current, out var neighbors))
                return;

            foreach (var edge in neighbors)
            {
                var edgeInterval = edge.ConnectionInterval;

                // Průnik dosavadní cesty s novou hranou
                var overlap = currentInterval.OverlappingInterval(edgeInterval);
                if (overlap is null)
                    continue;

                // Nalezena platná cesta do cíle
                if (edge.To.Id == endId)
                {
                    results.Add(new PathResult
                    {
                        Edges = new List<HSEdge2>(currentPath) { edge },
                        ValidInterval = overlap
                    });
                    continue; // nehledáme cesty PŘES cíl, ale hledáme další cesty DO cíle
                }

                // Detekce cyklu na aktuální cestě
                if (pathNodes.Contains(edge.To.Id))
                    continue;

                // Backtracking DFS
                pathNodes.Add(edge.To.Id);
                currentPath.Add(edge);

                Dfs(edge.To.Id, endId, overlap, adjacency, pathNodes, currentPath, results);

                currentPath.RemoveAt(currentPath.Count - 1);
                pathNodes.Remove(edge.To.Id);
            }
        }
    }
}
