using System.Collections.Generic;
using System.Linq;
using CommonUtilities;
using CommonUtilitiesTests.TestUtilities;
using Shouldly;
using Xunit;

namespace CommonUtilitiesTests
{
    public class GraphPathsTest
    {
        public static IEnumerable<object[]> FindAllPathsTestData
        {
            get
            {
                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2}}
                    },
                    new[] {1},
                    new[] {1},
                    new List<int[]> {new[] {1}}
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2}}
                    },
                    new[] {1},
                    new[] {2},
                    new List<int[]> {new[] {1, 2}}
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {3, 2}}
                    },
                    new[] {1},
                    new[] {2},
                    new List<int[]> {new[] {1, 2}}
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {3, 2}},
                        {3, new[] {2}}
                    },
                    new[] {1},
                    new[] {2},
                    new List<int[]>
                    {
                        new[] {1, 2},
                        new[] {1, 3, 2}
                    }
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2, 3}},
                        {2, new[] {4}},
                        {3, new[] {4}}
                    },
                    new[] {1},
                    new[] {4},
                    new List<int[]>
                    {
                        new[] {1, 2, 4},
                        new[] {1, 3, 4}
                    }
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {3, 4}},
                        {2, new[] {4, 5}},
                    },
                    new[] {2},
                    new[] {3},
                    new List<int[]>()
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {3}},
                        {2, new[] {3}},
                        {3, new[] {4, 5}}
                    },
                    new[] {1, 2},
                    new[] {4, 5},
                    new List<int[]>
                    {
                        new[] {1, 3, 4},
                        new[] {1, 3, 5},
                        new[] {2, 3, 4},
                        new[] {2, 3, 5}
                    }
                };

                // cycles are not handled
                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2, 3}},
                        {2, new[] {3, 1}}
                    },
                    new[] {1},
                    new[] {3},
                    new List<int[]>
                    {
                        new[] {1, 2, 3},
                        new[] {1, 3}
                    }
                };

                // cycles are not handled
                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2}},
                        {2, new[] {3, 1}},
                        {3, new[] {1}}
                    },
                    new[] {1, 2, 3},
                    new[] {1, 3},
                    new List<int[]>
                    {
                        new[] {1},
                        new[] {1, 2, 3},
                        new[] {2, 1},
                        new[] {2, 3},
                        new[] {2, 3, 1},
                        new[] {3},
                        new[] {3, 1}
                    }
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {2}},
                        {2, new[] {3}},
                        {3, new[] {4}}
                    },
                    new[] {1},
                    new[] {2, 3, 4},
                    new List<int[]>
                    {
                        new[] {1, 2},
                        new[] {1, 2, 3},
                        new[] {1, 2, 3, 4}
                    }
                };

                yield return new object[]
                {
                    new Dictionary<int, int[]>
                    {
                        {1, new[] {3}},
                        {2, new[] {3}},
                        {3, new[] {4}},
                        {5, new[] {4}},
                        {4, new[] {7, 6}}
                    },
                    new[] {1, 2, 5},
                    new[] {4, 6, 7},
                    new List<int[]>
                    {
                        new[] {1, 3, 4},
                        new[] {1, 3, 4, 6},
                        new[] {1, 3, 4, 7},
                        new[] {2, 3, 4},
                        new[] {2, 3, 4, 6},
                        new[] {2, 3, 4, 7},
                        new[] {5, 4},
                        new[] {5, 4, 6},
                        new[] {5, 4, 7}
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(FindAllPathsTestData))]
        public void FindAllPathsBetween(Dictionary<int, int[]> graphEdges, int[] startNodeIds, int[] endNodeIds, List<int[]> expectedPaths)
        {
            var graph = Graph.FromEdgeDictionary(graphEdges);
            var startNodes = IdsToNodes(startNodeIds, graph);
            var endNodes = IdsToNodes(endNodeIds, graph);

            var paths = GraphPaths.FindAllPathsBetween(startNodes, endNodes).ToArray();

            var pathLists =
                paths.Select(p => p.NodeList)
                    .Select(p => p.Select(n => ((Node) n).Value).ToArray())
                    .ToList();

            pathLists.ShouldBe(expectedPaths, true);
        }

        private static IEnumerable<Node> IdsToNodes(int[] startNodeIds, Graph graph)
        {
            return graph.Nodes.Where(n => startNodeIds.Contains(n.Value));
        }
    }
}
