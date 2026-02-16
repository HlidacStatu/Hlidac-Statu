using Devmasters.DT;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static HlidacStatu.DS.Graphs.Graph;

namespace HlidacStatu.DS.Graphs2
{

    public sealed record PathResult(
    IReadOnlyList<string> Nodes,
    IReadOnlyList<IEdge> Edges,
    DateInterval ValidInterval
);



    public class __UnweightedGraph
    {
        public __UnweightedGraph(IEnumerable<IVertex> initialNodes = null)
        {
            Vertices = initialNodes == null ? new HashSet<IVertex>() : initialNodes.ToHashSet();
        }

        public HashSet<IVertex> Vertices { get; }
        public IEnumerable<IEdge> Edges { get => Vertices.SelectMany(v => v.OutgoingEdges); }

        /// <summary>
        /// Add new directed unweighted edge to graph. If Vertices (from, to) doesn't exist. It adds them too.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        public void AddEdge<T>(IVertex from, IVertex to, DateInterval connectionInterval, T bindingPayload)
        {
            IVertex vertex1 = GetOrAddVertex(from);
            IVertex vertex2 = GetOrAddVertex(to);

            //dont like this, but can't think better solution right now
            var edge = new Edge<T>(vertex1, vertex2, connectionInterval, bindingPayload);

            // There can be only one direct outgoing edge from A to B
            // Other outgoing edges from A to B are skipped in graph
            vertex1.AddOutgoingEdge(edge);
        }

        object lockAddVert = new object();
        private IVertex GetOrAddVertex(IVertex vertex)
        {
            if (Vertices.TryGetValue(vertex, out IVertex actual))
            {
                return actual;
            }
            lock (lockAddVert)
            {
                Vertices.Add(vertex);
            }
            return (vertex);
        }


        /// <summary>
        /// Finds shortest path from point A to point B.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        /// <returns>Edges in order along the path (from => to)</returns>
        public IEnumerable<IEdge> ShortestPath2(IVertex from, IVertex to, DateTime? fromD, DateInterval interval = null)
        {

            bool fromExists = Vertices.TryGetValue(from, out from);
            bool toExists = Vertices.TryGetValue(to, out to);
            if (!fromExists || !toExists)
                return null;

            var visitHistory = new List<IEdge>();
            var visitedVertices = new HashSet<IVertex>();

            Queue<IVertex> queuedVertices = new Queue<IVertex>();
            queuedVertices.Enqueue(from);

            while (queuedVertices.Count > 0)
            {
                var currentVertex = queuedVertices.Dequeue();
                visitedVertices.Add(currentVertex);

                foreach (var edge in currentVertex.OutgoingEdges)
                {
                    if (visitedVertices.Contains(edge.To))
                        continue;

                    visitHistory.Add(edge);
                    queuedVertices.Enqueue(edge.To);

                    bool isSearchedVertex = edge.To == to;
                    if (isSearchedVertex)
                        return GetPath(from, to, visitHistory);

                }
            }

            return null;
        }



        /// <summary>
        /// Finds shortest path from point A to point B.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        /// <returns>Edges in order along the path (from => to)</returns>
        public PathResult ShortestPath(IVertex from, IVertex to, DateInterval interval = null)
        {
            IEnumerable<IEdge> edges = this.Edges;
            return ShortestPath(edges, from, to, interval);
        }
        public static PathResult ShortestPath(IEnumerable<IEdge> edges, IVertex from, IVertex to, DateInterval interval = null)
        {
            
            string fromId = from.Id;
            string toId = to.Id;
            DateInterval? requiredInterval = interval ?? new DateInterval((DateTime?)null, (DateTime?)null);

            if (fromId is null) throw new ArgumentNullException(nameof(fromId));
            if (toId is null) throw new ArgumentNullException(nameof(toId));
            if (edges is null) throw new ArgumentNullException(nameof(edges));


            // adjacency: From -> outgoing edges
            var adj = edges
                .GroupBy(e => e.From.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            // Start interval: unbounded, optionally intersect with requiredInterval
            var start = requiredInterval;


            // BFS over (node, interval)
            var q = new Queue<int>();

            var statesNode = new List<string>();
            var statesInterval = new List<DateInterval>();
            var parentState = new List<int>();          // -1 = root
            var parentEdge = new List<IEdge>();         // null = root


            int NewState(string node, DateInterval interval, int parent, IEdge via)
            {
                int id = statesNode.Count;
                statesNode.Add(node);
                statesInterval.Add(interval);
                parentState.Add(parent);
                parentEdge.Add(via);
                return id;
            }
            // visited pruning: for each node keep list of reached intervals.
            // If new interval is subset of any already reached interval => skip.
            // If new interval strictly contains some existing intervals => remove them.
            var visited = new Dictionary<string, List<DateInterval>>(StringComparer.Ordinal);
            bool TryAddVisited(string node, DateInterval interval)
            {
                if (!visited.TryGetValue(node, out var list))
                {
                    visited[node] = new List<DateInterval> { interval };
                    return true;
                }

                // If covered by an existing interval -> no need to explore
                foreach (var existing in list)
                {
                    if (ContainsDI(existing, interval))
                        return false;
                }

                // Remove intervals that are covered by the new one (keep list minimal)
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (ContainsDI(interval, list[i]))
                        list.RemoveAt(i);
                }

                list.Add(interval);
                return true;
            }

            // root
            int root = NewState(fromId, start, -1, null);
            TryAddVisited(fromId, start);
            q.Enqueue(root);

            while (q.Count > 0)
            {
                int sid = q.Dequeue();
                var node = statesNode[sid];
                var curInterval = statesInterval[sid];

                if (StringComparer.Ordinal.Equals(node, toId))
                    return BuildResult(sid, statesNode, statesInterval, parentState, parentEdge);

                if (!adj.TryGetValue(node, out var outgoing))
                    continue;

                foreach (var e in outgoing)
                {
                    var edgeInterval = e.ConnectionInterval;

                    var nextInterval = curInterval.OverlappingInterval(edgeInterval);
                    if (nextInterval is null) continue;

                    if (requiredInterval is not null)
                    {
                        nextInterval = nextInterval.OverlappingInterval(requiredInterval);
                        if (nextInterval is null) continue;
                    }

                    var nextNode = e.To.Id;
                    if (!TryAddVisited(nextNode, nextInterval)) continue;

                    int nid = NewState(nextNode, nextInterval, sid, e);
                    q.Enqueue(nid);
                }
            }

            return null;
        }
        private static PathResult BuildResult(
        int lastStateId,
        List<string> nodes,
        List<DateInterval> intervals,
        List<int> parentState,
        List<IEdge> parentEdge)
        {
            var pathEdges = new List<IEdge>();
            var pathNodes = new List<string>();

            int cur = lastStateId;
            while (cur != -1)
            {
                pathNodes.Add(nodes[cur]);
                var e = parentEdge[cur];
                if (e is not null) pathEdges.Add(e);
                cur = parentState[cur];
            }

            pathNodes.Reverse();
            pathEdges.Reverse();

            return new PathResult(
                Nodes: pathNodes,
                Edges: pathEdges,
                ValidInterval: intervals[lastStateId]
            );
        }
        // interval A contains interval B (with null = -inf/+inf)
        private static bool ContainsDI(DateInterval a, DateInterval b)
        {
            // a.From <= b.From
            bool fromOk = !b.From.HasValue
                ? !a.From.HasValue
                : (!a.From.HasValue || a.From.Value <= b.From.Value);

            // a.To >= b.To
            bool toOk = !b.To.HasValue
                ? !a.To.HasValue
                : (!a.To.HasValue || a.To.Value >= b.To.Value);

            return fromOk && toOk;
        }


        /// <summary>
        /// Function to find path in visitHistory
        /// </summary>
        private IEnumerable<IEdge> GetPath(IVertex from, IVertex to, List<IEdge> visitHistory)
        {
            var results = new List<IEdge>();
            var previousVertex = to;

            do
            {
                var edge = visitHistory.Where(e => e.To.Equals(previousVertex)).FirstOrDefault();
                results.Add(edge);
                previousVertex = edge.From;

            } while (!previousVertex.Equals(from));

            return results.Reverse<IEdge>();
        }


        /// <summary>
        /// Projde (do šířky) všechny vrcholy od konkrétního bodu a postupně je vypíše.
        /// </summary>
        /// <param name="from">Vrchol, ze kterého se bude graf procházet</param>
        /// <returns></returns>
        public IEnumerable<IVertex> BreathFirstIterator(IVertex from)
        {
            var visitedVertices = new HashSet<IVertex>();

            Queue<IVertex> queuedVertices = new Queue<IVertex>();
            queuedVertices.Enqueue(from);

            while (queuedVertices.Count > 0)
            {
                var currentVertex = queuedVertices.Dequeue();
                visitedVertices.Add(currentVertex);
                yield return currentVertex;

                foreach (var edge in currentVertex.OutgoingEdges)
                {
                    if (!visitedVertices.Contains(edge.To))
                    {
                        queuedVertices.Enqueue(edge.To);
                    }
                }
            }
        }

    }
}
