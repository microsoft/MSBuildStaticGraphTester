using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using CommonUtilities;

namespace CommonUtilitiesTests.TestUtilities
{
    [DebuggerDisplay(@"{DebugString()}")]
    public class Graph
    {
        public IReadOnlyCollection<Node> Nodes { get; }

        private Graph(IReadOnlyCollection<Node> nodes)
        {
            Nodes = nodes;
        }

        public static Graph FromEdgeDictionary(
            // direct dependencies that the kvp.key node has on the nodes represented by kvp.value
            IDictionary<int, int[]> dependencyEdges)
        {
            var nodes = new ConcurrentDictionary<int, Node>();

            foreach (var dependencyEdge in dependencyEdges)
            {
                var parentNode = GetNode(dependencyEdge.Key);

                foreach (var child in dependencyEdge.Value)
                {
                    var childNode = GetNode(child);

                    parentNode.AddChild(childNode);
                }
            }

            return new Graph(nodes.Values.ToImmutableArray());

            Node GetNode(int nodeId)
            {
                return nodes.GetOrAdd(nodeId, i => new Node(i));
            }
        }

        private string DebugString()
        {
            return
                $"#nodes={Nodes.Count}";
        }
    }

    [DebuggerDisplay(@"{DebugString()}")]
    public class Node : GraphPaths.IGraphNode
    {
        private readonly List<Node> _children;
        private readonly List<Node> _parents;

        public int Value { get; }

        public Node(int value)
        {
            Value = value;
            _children = new List<Node>();
            _parents = new List<Node>();
        }

        public IReadOnlyCollection<GraphPaths.IGraphNode> Parents => _parents;

        public IReadOnlyCollection<GraphPaths.IGraphNode> Children => _children;

        public void AddChild(Node childNode)
        {
            _children.Add(childNode);
            childNode._parents.Add(this);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        private string DebugString()
        {
            return
                $"{Value}, #in={Parents.Count}, #out={Children.Count}";
        }
    }
}
