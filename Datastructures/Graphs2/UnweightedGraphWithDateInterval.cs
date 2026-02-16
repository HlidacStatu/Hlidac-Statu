using Devmasters.DT;
using Serilog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HlidacStatu.DS.Graphs2
{
    public class UnweightedGraph_imp2
    {
        private static readonly ILogger _logger = Log.ForContext<UnweightedGraph>();

        public UnweightedGraph_imp2(IEnumerable<IVertex> initialNodes = null)
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
        /// Finds shortest path from point A to point B, considering DateInterval overlap.
        /// Only edges whose ConnectionInterval overlaps with the given interval are traversed.
        /// The resulting ValidInterval is the intersection of all edge intervals along the path.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        /// <param name="interval">Required date interval filter. If null, all edges are considered.</param>
        /// <returns>PathResult with nodes, edges, and valid interval; or null if no path found.</returns>
        public PathResult ShortestPath(IVertex from, IVertex to, DateInterval interval = null)
        {
            //bool fromExists = Vertices.TryGetValue(from, out from);
            //bool toExists = Vertices.TryGetValue(to, out to);

            //if (!fromExists && !toExists)
            //{
            //    _logger.Debug("ShortestPath: neither vertex found in graph. From={FromId}, To={ToId}",
            //        from?.Id, to?.Id);
            //    return null;
            //}
            //if (!fromExists)
            //{
            //    _logger.Debug("ShortestPath: source vertex {FromId} not found in graph", from?.Id);
            //    return null;
            //}
            //if (!toExists)
            //{
            //    _logger.Debug("ShortestPath: destination vertex {ToId} not found in graph", to?.Id);
            //    return null;
            //}

            return ShortestPath(Edges, from, to, interval);
        }

        /// <summary>
        /// BFS state representing a node reached with a specific valid date interval.
        /// Each state tracks its parent for path reconstruction.
        /// </summary>
        private class BfsState
        {
            public string NodeId { get; }
            public DateInterval ValidInterval { get; }
            public int ParentIndex { get; }
            public IEdge ParentEdge { get; }

            public BfsState(string nodeId, DateInterval validInterval, int parentIndex, IEdge parentEdge)
            {
                NodeId = nodeId;
                ValidInterval = validInterval;
                ParentIndex = parentIndex;
                ParentEdge = parentEdge;
            }
        }

        public static PathResult ShortestPath(
            IEnumerable<IEdge> edges, IVertex from, IVertex to, DateInterval interval = null)
        {
            string fromId = from.Id;
            string toId = to.Id;

            // When no interval filter is provided, use an unbounded interval (matches everything)
            DateInterval requiredInterval = interval ?? new DateInterval((DateTime?)null, (DateTime?)null);

            _logger.Debug("ShortestPath: searching from {FromId} to {ToId}, interval {IntervalFrom} - {IntervalTo}",
                fromId, toId, requiredInterval.From, requiredInterval.To);

            // Build adjacency list: node ID -> list of outgoing edges
            var adjacency = edges
                .GroupBy(e => e.From.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            // Check early: if source has no outgoing edges at all, no path is possible
            if (!adjacency.ContainsKey(fromId))
            {
                _logger.Debug("ShortestPath: source {FromId} has no outgoing edges", fromId);
                return null;
            }

            // All BFS states stored in a flat list; indices serve as state IDs
            var states = new List<BfsState>();

            // Visited pruning: for each node, keep a list of intervals already reached.
            // This avoids re-exploring a node with a narrower (subset) interval
            // when a wider one was already processed.
            var visited = new Dictionary<string, List<DateInterval>>(StringComparer.Ordinal);

            // Counters for logging skipped edges
            int skippedNoOverlap = 0;
            int skippedAlreadyVisited = 0;

            // Seed BFS with the starting node and the required interval
            var rootState = new BfsState(fromId, requiredInterval, -1, null);
            states.Add(rootState);
            visited[fromId] = new List<DateInterval> { requiredInterval };

            var queue = new Queue<int>();
            queue.Enqueue(0);

            // BFS main loop - process states level by level (guarantees shortest path)
            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();
                var current = states[currentIndex];

                // Check if we reached the destination
                if (StringComparer.Ordinal.Equals(current.NodeId, toId))
                {
                    var result = ReconstructPath(currentIndex, states);
                    _logger.Debug("ShortestPath: path found from {FromId} to {ToId}, " +
                        "{HopCount} hops, valid interval {ValidFrom} - {ValidTo}",
                        fromId, toId, result.Edges.Count,
                        result.ValidInterval.From, result.ValidInterval.To);
                    return result;
                }

                // Skip nodes with no outgoing edges
                if (!adjacency.TryGetValue(current.NodeId, out var outgoing))
                    continue;

                // Try each outgoing edge from the current node
                foreach (var edge in outgoing)
                {
                    // Compute the intersection of the current path interval with the edge interval.
                    // If there is no overlap, this edge is not valid for the requested time period.
                    var nextInterval = current.ValidInterval.OverlappingInterval(edge.ConnectionInterval);
                    if (nextInterval is null)
                    {
                        skippedNoOverlap++;
                        _logger.Verbose("ShortestPath: edge {FromId}->{ToId} skipped, " +
                            "no interval overlap (edge: {EdgeFrom}-{EdgeTo}, current: {CurFrom}-{CurTo})",
                            edge.From.Id, edge.To.Id,
                            edge.ConnectionInterval.From, edge.ConnectionInterval.To,
                            current.ValidInterval.From, current.ValidInterval.To);
                        continue;
                    }

                    // When an explicit interval filter was provided, also intersect with it
                    // to ensure the path never exceeds the requested bounds
                    if (interval is not null)
                    {
                        nextInterval = nextInterval.OverlappingInterval(requiredInterval);
                        if (nextInterval is null)
                        {
                            skippedNoOverlap++;
                            continue;
                        }
                    }

                    // Visited pruning: skip if this node was already reached with a wider interval
                    if (!TryAddVisited(visited, edge.To.Id, nextInterval))
                    {
                        skippedAlreadyVisited++;
                        continue;
                    }

                    // Register the new BFS state and enqueue it
                    var nextState = new BfsState(edge.To.Id, nextInterval, currentIndex, edge);
                    int nextIndex = states.Count;
                    states.Add(nextState);
                    queue.Enqueue(nextIndex);
                }
            }

            // No path found - log diagnostic summary
            _logger.Debug("ShortestPath: no path found from {FromId} to {ToId}. " +
                "Explored {ExploredCount} states, skipped {SkippedNoOverlap} edges (no interval overlap), " +
                "skipped {SkippedVisited} edges (already visited with wider interval), " +
                "reached {ReachedNodes} unique nodes",
                fromId, toId, states.Count, skippedNoOverlap, skippedAlreadyVisited, visited.Count);
            return null;
        }

        /// <summary>
        /// Attempts to register a new visited interval for a node.
        /// Returns false if the node was already visited with an interval that fully covers the new one.
        /// If the new interval is wider, removes the narrower existing entries.
        /// </summary>
        private static bool TryAddVisited(
            Dictionary<string, List<DateInterval>> visited, string nodeId, DateInterval interval)
        {
            if (!visited.TryGetValue(nodeId, out var existingIntervals))
            {
                // First visit to this node
                visited[nodeId] = new List<DateInterval> { interval };
                return true;
            }

            // If any existing interval already covers the new one, skip (no new information)
            foreach (var existing in existingIntervals)
            {
                if (ContainsDI(existing, interval))
                    return false;
            }

            // Remove existing intervals that are fully covered by the new wider one
            for (int i = existingIntervals.Count - 1; i >= 0; i--)
            {
                if (ContainsDI(interval, existingIntervals[i]))
                    existingIntervals.RemoveAt(i);
            }

            existingIntervals.Add(interval);
            return true;
        }

        /// <summary>
        /// Walks the parent chain from the destination state back to the root
        /// and builds the PathResult with nodes, edges, and the resulting valid interval.
        /// </summary>
        private static PathResult ReconstructPath(int destinationIndex, List<BfsState> states)
        {
            var pathEdges = new List<IEdge>();
            var pathNodes = new List<string>();

            // Walk backwards from destination to root via parent indices
            int cur = destinationIndex;
            while (cur != -1)
            {
                var state = states[cur];
                pathNodes.Add(state.NodeId);
                if (state.ParentEdge is not null)
                    pathEdges.Add(state.ParentEdge);
                cur = state.ParentIndex;
            }

            // Reverse to get the correct order (from -> to)
            pathNodes.Reverse();
            pathEdges.Reverse();

            return new PathResult(
                Nodes: pathNodes,
                Edges: pathEdges,
                ValidInterval: states[destinationIndex].ValidInterval
            );
        }

        /// <summary>
        /// Checks whether interval A fully contains interval B.
        /// null values represent unbounded ends (null From = -infinity, null To = +infinity).
        /// </summary>
        private static bool ContainsDI(DateInterval a, DateInterval b)
        {
            // a.From <= b.From (null From means -infinity, so it always satisfies <=)
            bool fromOk = !b.From.HasValue
                ? !a.From.HasValue
                : (!a.From.HasValue || a.From.Value <= b.From.Value);

            // a.To >= b.To (null To means +infinity, so it always satisfies >=)
            bool toOk = !b.To.HasValue
                ? !a.To.HasValue
                : (!a.To.HasValue || a.To.Value >= b.To.Value);

            return fromOk && toOk;
        }


    }
}
