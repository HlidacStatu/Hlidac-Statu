using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        /// Add new directed unweighted edge to graph. If Vertices (from, to) doesn't exist. It adds them too.
        /// </summary>
        /// <param name="from">Starting vertex</param>
        /// <param name="to">Destination vertex</param>
        public void AddEdge<T>(IVertex from, IVertex to, T bindingPayload)
        {
            IVertex vertex1 = GetOrAddVertex(from);
            IVertex vertex2 = GetOrAddVertex(to);

            //dont like this, but can't think better solution right now
            var edge = new Edge<T>(vertex1, vertex2, bindingPayload);

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
        public IEnumerable<IEdge> ShortestPath(IVertex from, IVertex to)
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
