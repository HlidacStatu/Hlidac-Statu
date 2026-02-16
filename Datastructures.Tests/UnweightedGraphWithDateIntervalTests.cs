using System;
using System.Linq;
using Devmasters.DT;
using HlidacStatu.DS.Graphs2;
using Xunit;

namespace Datastructures.Tests
{
    public class UnweightedGraphTests
    {
        // Helper to create vertices with string IDs
        private static IVertex V(string id) => new Vertex<string>(id, id);

        // Helper to create DateInterval from year range
        private static DateInterval DI(int fromYear, int toYear) =>
            new DateInterval(new DateTime(fromYear, 1, 1), new DateTime(toYear, 12, 31));

        // Unbounded interval
        private static DateInterval Unbounded() =>
            new DateInterval((DateTime?)null, (DateTime?)null);

        #region Successful path searches

        [Fact]
        public void ShortestPath_DirectEdge_WithMatchingInterval_ReturnsPath()
        {
            // A --[2020-2025]--> B
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, DI(2020, 2025), "link");

            var result = graph.ShortestPath(V("A"), V("B"), DI(2021, 2023));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B" }, result.Nodes);
            Assert.Single(result.Edges);
            Assert.NotNull(result.ValidInterval);
        }

        [Fact]
        public void ShortestPath_TwoHops_WithOverlappingIntervals_ReturnsPath()
        {
            // A --[2018-2025]--> B --[2020-2030]--> C
            // Searching with interval [2021-2024] should find path A->B->C
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            var c = V("C");
            graph.AddEdge(a, b, DI(2018, 2025), "ab");
            graph.AddEdge(b, c, DI(2020, 2030), "bc");

            var result = graph.ShortestPath(V("A"), V("C"), DI(2021, 2024));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B", "C" }, result.Nodes);
            Assert.Equal(2, result.Edges.Count);
            // Valid interval should be intersection of all: [2021, 2024]
            Assert.True(result.ValidInterval.From >= new DateTime(2021, 1, 1));
            Assert.True(result.ValidInterval.To <= new DateTime(2024, 12, 31));
        }

        [Fact]
        public void ShortestPath_WithoutInterval_UsesAllEdges()
        {
            // A --[2020-2025]--> B
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, DI(2020, 2025), "link");

            var result = graph.ShortestPath(V("A"), V("B"));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B" }, result.Nodes);
        }

        [Fact]
        public void ShortestPath_ChoosesShorterPath_WhenBothMatchInterval()
        {
            // A --[2020-2030]--> B --[2020-2030]--> D
            // A --[2020-2030]--> C --[2020-2030]--> E --[2020-2030]--> D
            // BFS should find A->B->D (2 hops) before A->C->E->D (3 hops)
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            var c = V("C");
            var d = V("D");
            var e = V("E");
            graph.AddEdge(a, b, DI(2020, 2030), "ab");
            graph.AddEdge(b, d, DI(2020, 2030), "bd");
            graph.AddEdge(a, c, DI(2020, 2030), "ac");
            graph.AddEdge(c, e, DI(2020, 2030), "ce");
            graph.AddEdge(e, d, DI(2020, 2030), "ed");

            var result = graph.ShortestPath(V("A"), V("D"), DI(2022, 2025));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B", "D" }, result.Nodes);
            Assert.Equal(2, result.Edges.Count);
        }

        [Fact]
        public void ShortestPath_ParallelPaths_PicksValidOne()
        {
            // A --[2010-2015]--> B --[2010-2015]--> D (expired path)
            // A --[2020-2025]--> C --[2020-2025]--> D (valid path)
            // Searching for [2022-2023] should find path via C
            var graph = new UnweightedGraph();
            graph.AddEdge(V("A"), V("B"), DI(2010, 2015), "ab");
            graph.AddEdge(V("B"), V("D"), DI(2010, 2015), "bd");
            graph.AddEdge(V("A"), V("C"), DI(2020, 2025), "ac");
            graph.AddEdge(V("C"), V("D"), DI(2020, 2025), "cd");

            var result = graph.ShortestPath(V("A"), V("D"), DI(2022, 2023));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "C", "D" }, result.Nodes);
        }

        [Fact]
        public void ShortestPath_UnboundedEdgeIntervals_AlwaysMatch()
        {
            // A --[null,null]--> B (unbounded edge)
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, Unbounded(), "link");

            var result = graph.ShortestPath(V("A"), V("B"), DI(2022, 2023));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B" }, result.Nodes);
        }

        #endregion

        #region Failed path searches

        [Fact]
        public void ShortestPath_NoOverlap_ReturnsNull()
        {
            // A --[2020-2022]--> B, searching for [2025-2026]
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, DI(2020, 2022), "link");

            var result = graph.ShortestPath(V("A"), V("B"), DI(2025, 2026));

            Assert.Null(result);
        }

        [Fact]
        public void ShortestPath_PartialChainBreak_ReturnsNull()
        {
            // A --[2020-2025]--> B --[2010-2015]--> C
            // Searching [2022-2024]: A->B works, but B->C doesn't overlap
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            var c = V("C");
            graph.AddEdge(a, b, DI(2020, 2025), "ab");
            graph.AddEdge(b, c, DI(2010, 2015), "bc");

            var result = graph.ShortestPath(V("A"), V("C"), DI(2022, 2024));

            Assert.Null(result);
        }

        [Fact]
        public void ShortestPath_VertexNotInGraph_ReturnsNull()
        {
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, DI(2020, 2025), "link");

            var result = graph.ShortestPath(V("A"), V("X"), DI(2022, 2023));

            Assert.Null(result);
        }

        [Fact]
        public void ShortestPath_NoEdges_ReturnsNull()
        {
            // A and B exist but no edge between them
            var graph = new UnweightedGraph();
            graph.AddEdge(V("A"), V("C"), DI(2020, 2025), "ac");
            graph.AddEdge(V("D"), V("B"), DI(2020, 2025), "db");

            var result = graph.ShortestPath(V("A"), V("B"), DI(2022, 2023));

            Assert.Null(result);
        }

        [Fact]
        public void ShortestPath_SameStartAndEnd_ReturnsImmediately()
        {
            var graph = new UnweightedGraph();
            var a = V("A");
            var b = V("B");
            graph.AddEdge(a, b, DI(2020, 2025), "ab");

            var result = graph.ShortestPath(V("A"), V("A"), DI(2022, 2023));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A" }, result.Nodes);
            Assert.Empty(result.Edges);
        }

        [Fact]
        public void ShortestPath_NarrowingIntervalAlongPath_ReturnsCorrectValidInterval()
        {
            // A --[2015-2025]--> B --[2018-2023]--> C --[2020-2022]--> D
            // Searching [2019-2024]: valid interval should narrow to [2020-2022]
            var graph = new UnweightedGraph();
            graph.AddEdge(V("A"), V("B"), DI(2015, 2025), "ab");
            graph.AddEdge(V("B"), V("C"), DI(2018, 2023), "bc");
            graph.AddEdge(V("C"), V("D"), DI(2020, 2022), "cd");

            var result = graph.ShortestPath(V("A"), V("D"), DI(2019, 2024));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B", "C", "D" }, result.Nodes);
            Assert.Equal(3, result.Edges.Count);
            // The valid interval should be narrowed to [2020, 2022]
            Assert.True(result.ValidInterval.From >= new DateTime(2020, 1, 1));
            Assert.True(result.ValidInterval.To <= new DateTime(2022, 12, 31));
        }

        [Fact]
        public void ShortestPath_DirectPathBlocked_LongerPathAvailable_FindsLongerPath()
        {
            // A --[2010-2012]--> C (blocked by interval)
            // A --[2020-2025]--> B --[2020-2025]--> C (valid)
            // Searching [2022-2024]
            var graph = new UnweightedGraph();
            graph.AddEdge(V("A"), V("C"), DI(2010, 2012), "ac-old");
            graph.AddEdge(V("A"), V("B"), DI(2020, 2025), "ab");
            graph.AddEdge(V("B"), V("C"), DI(2020, 2025), "bc");

            var result = graph.ShortestPath(V("A"), V("C"), DI(2022, 2024));

            Assert.NotNull(result);
            Assert.Equal(new[] { "A", "B", "C" }, result.Nodes);
            Assert.Equal(2, result.Edges.Count);
        }

        #endregion
    }
}
