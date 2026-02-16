using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Devmasters.DT;

namespace HlidacStatu.DS.Graphs2
{
    // TODO - temporary, use later Devmasters.Collections.ConcurrentHashSet from Devmasters.Collections v10 package
    public sealed class ConcurrentHashSet2<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, T> _dict;

        public ConcurrentHashSet2() : this(comparer: null) { }

        public ConcurrentHashSet2(IEqualityComparer<T>? comparer)
            => _dict = new ConcurrentDictionary<T, T>(comparer ?? EqualityComparer<T>.Default);

        public ConcurrentHashSet2(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null)
            : this(comparer)
        {
            // Avoid ConcurrentDictionary ctor throwing on duplicate keys in 'collection'
            foreach (var item in collection)
                _dict.TryAdd(item, item);
        }

        public int Count => _dict.Count;

        public bool Add(T item) => _dict.TryAdd(item, item);

        public bool Contains(T item) => _dict.ContainsKey(item);

        public bool Remove(T item) => _dict.TryRemove(item, out _);

        public void Clear() => _dict.Clear();

        // Returns the canonical stored instance (if you care about that)
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
            => _dict.TryGetValue(equalValue, out actualValue);

        // If you keep this, at least make it explicit and non-null-forgiving
        public bool TryGet(T equalValue, out T actualValue)
            => _dict.TryGetValue(equalValue, out actualValue);

        public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static ConcurrentHashSet2<TSource> ToConcurrentHashSet<TSource>(
            IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return new ConcurrentHashSet2<TSource>(source, comparer);
        }
    }

    public class UnweightedGraph
    {
        public UnweightedGraph(IEnumerable<IVertex> initialNodes = null)
        {
            Vertices = initialNodes == null ? new ConcurrentHashSet2<IVertex>() : ConcurrentHashSet2<IVertex>.ToConcurrentHashSet(initialNodes) ;
        }

        public ConcurrentHashSet2<IVertex> Vertices { get; }
        public IEnumerable<IEdge> Edges { get => Vertices.SelectMany(v => v.OutgoingEdges); }

        /// <summary>
        /// Add new directed unweighted edge to graph. If Vertices (from, to) doesn't exist, it adds them too.
        /// The edge includes a DateInterval representing the time period during which the connection is valid.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        /// <param name="connectionInterval">Time interval during which this edge is active/valid</param>
        /// <param name="bindingPayload">Additional data attached to the edge</param>
        public void AddEdge<T>(IVertex from, IVertex to, DateInterval connectionInterval, T bindingPayload)
        {
            IVertex vertex1 = GetOrAddVertex(from);
            IVertex vertex2 = GetOrAddVertex(to);

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
        /// Finds shortest path from point A to point B using BFS.
        /// Only traverses edges whose ConnectionInterval contains the specified date.
        /// If dateToCheck is null, all edges are considered (no date filtering).
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        /// <param name="dateToCheck">
        /// Date used to filter edges. Only edges whose ConnectionInterval includes this date are traversed.
        /// When null, all edges are traversed regardless of their interval.
        /// </param>
        /// <returns>Edges in order along the path (from => to), or null if no path exists</returns>
        public IEnumerable<IEdge> ShortestPath(IVertex from, IVertex to, DateTime? dateToCheck = null)
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

                    // Skip edges that are not active at the specified date.
                    // An edge is considered active when:
                    //   - dateToCheck is null (no filtering, traverse all edges), or
                    //   - the edge's ConnectionInterval contains the given date
                    if (dateToCheck.HasValue && !IsEdgeActiveAtDate(edge, dateToCheck.Value))
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
        /// Checks whether an edge is active (valid) at the given date.
        /// Uses the edge's ConnectionInterval.IsDateInInterval method.
        /// If the edge has no interval set (both From and To are null), it is always considered active.
        /// </summary>
        /// <param name="edge">The edge to check</param>
        /// <param name="date">The date to check against the edge's interval</param>
        /// <returns>True if the edge is active at the given date</returns>
        private bool IsEdgeActiveAtDate(IEdge edge, DateTime date)
        {
            // If there is no interval defined, the edge is always active
            if (edge.ConnectionInterval == null)
                return true;

            // If both bounds are null, the interval is unbounded - edge is always active
            if (!edge.ConnectionInterval.From.HasValue && !edge.ConnectionInterval.To.HasValue)
                return true;

            return edge.ConnectionInterval.IsDateInInterval(date);
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
        /// Breadth-first traversal of all vertices from a given starting point.
        /// Only traverses edges whose ConnectionInterval contains the specified date.
        /// If dateToCheck is null, all edges are traversed (no date filtering).
        /// </summary>
        /// <param name="from">Starting vertex for the traversal</param>
        /// <param name="dateToCheck">
        /// Date used to filter edges. Only edges active at this date are followed.
        /// When null, all edges are followed regardless of their interval.
        /// </param>
        /// <returns>Vertices in breadth-first order</returns>
        public IEnumerable<IVertex> BreathFirstIterator(IVertex from, DateTime? dateToCheck = null)
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
                    // Skip edges not active at the specified date
                    if (dateToCheck.HasValue && !IsEdgeActiveAtDate(edge, dateToCheck.Value))
                        continue;

                    if (!visitedVertices.Contains(edge.To))
                    {
                        queuedVertices.Enqueue(edge.To);
                    }
                }
            }
        }

    }
}
