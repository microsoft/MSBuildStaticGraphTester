using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Graph;

namespace CommonUtilities
{
    public class GraphPaths
    {
        public interface IGraphNode
        {
            IReadOnlyCollection<IGraphNode> Parents { get; }
            IReadOnlyCollection<IGraphNode> Children { get; }
        }

        [DebuggerDisplay(@"{DebugString()}")]
        public class Path
        {
            private readonly List<IGraphNode> _path;

            public IGraphNode EndNode => _path.Last();
            public IReadOnlyList<IGraphNode> NodeList => _path.ToImmutableArray();

            public Path(IGraphNode startNode)
            {
                _path = new List<IGraphNode> {startNode};
            }

            private Path(List<IGraphNode> path)
            {
                _path = path;
            }

            internal void AddNode(IGraphNode node)
            {
                _path.Add(node);
            }

            public Path Clone()
            {
                return new Path(new List<IGraphNode>(_path));
            }

            public bool Contains(IGraphNode node)
            {
                return _path.Contains(node);
            }

            private string DebugString()
            {
                return
                    $"{string.Join(",", _path.Select(n => n.ToString()))}";
            }
        }

        internal class ProjectGraphNodeAdapter : IGraphNode
        {
            internal class NodeProvider
            {
                private readonly ConcurrentDictionary<ProjectGraphNode, ProjectGraphNodeAdapter> _nodePool =
                    new ConcurrentDictionary<ProjectGraphNode, ProjectGraphNodeAdapter>();

                public ProjectGraphNodeAdapter FromProjectGraphNode(ProjectGraphNode node)
                {
                    return _nodePool.GetOrAdd(node, pgn => new ProjectGraphNodeAdapter(pgn, this));
                }
            }

            private readonly Lazy<IReadOnlyCollection<IGraphNode>> _children;
            private readonly Lazy<IReadOnlyCollection<IGraphNode>> _parents;
            public ProjectGraphNode AdaptedNode { get; }

            private ProjectGraphNodeAdapter(ProjectGraphNode adaptedNode, NodeProvider nodeProvider)
            {
                AdaptedNode = adaptedNode;
                _parents =
                    new Lazy<IReadOnlyCollection<IGraphNode>>(
                        () => adaptedNode.ReferencingProjects.Select(n => nodeProvider.FromProjectGraphNode(n)).ToArray());
                _children =
                    new Lazy<IReadOnlyCollection<IGraphNode>>(
                        () => adaptedNode.ProjectReferences.Select(n => nodeProvider.FromProjectGraphNode(n)).ToArray());
            }

            public IReadOnlyCollection<IGraphNode> Parents => _parents.Value;
            public IReadOnlyCollection<IGraphNode> Children => _children.Value;
        }

        public static IEnumerable<Path> FindAllPathsBetween(IEnumerable<IGraphNode> startNodes, IEnumerable<IGraphNode> endNodes)
        {
            Trace.Assert(startNodes != null);
            Trace.Assert(endNodes != null);

            var endNodesSet = endNodes.ToHashSet();

            Trace.Assert(endNodesSet.Count > 0);

            var exploratoryPaths = new Queue<Path>(startNodes.Select(n => new Path(n)));

            Trace.Assert(exploratoryPaths.Count > 0);

            var matchingPaths = new List<Path>();

            while (exploratoryPaths.Count > 0)
            {
                var currentPath = exploratoryPaths.Peek();

                var lastNodeInPath = currentPath.EndNode;

                if (endNodesSet.Contains(lastNodeInPath))
                {
                    // found matching path, save a clone, but do not remove from queue as this end node might point towards other end nodes
                    matchingPaths.Add(currentPath.Clone());
                }

                // do not follow children that would lead to a cycle in the path
                var children = lastNodeInPath.Children.Where(c => !currentPath.Contains(c)).ToArray();

                if (children.Length == 0)
                {
                    // found a dead end, remove path
                    exploratoryPaths.Dequeue();
                }
                else
                {
                    // grow current path with one child and fork other paths for the other children
                    foreach (var child in children.Skip(1))
                    {
                        var newPath = currentPath.Clone();
                        newPath.AddNode(child);

                        exploratoryPaths.Enqueue(newPath);
                    }

                    currentPath.AddNode(children.First());
                }
            }

            return matchingPaths;
        }

        public static IEnumerable<ImmutableArray<ProjectGraphNode>> FindAllPathsBetween(
            IEnumerable<ProjectGraphNode> startNodes,
            IEnumerable<ProjectGraphNode> endNodes)
        {
            var nodeProvider = new ProjectGraphNodeAdapter.NodeProvider();

            var adaptedStartNodes = startNodes.Select(s => nodeProvider.FromProjectGraphNode(s));
            var adaptedEndNodes = endNodes.Select(e => nodeProvider.FromProjectGraphNode(e));

            var paths = FindAllPathsBetween(adaptedStartNodes, adaptedEndNodes);

            return paths.Select(p => p.NodeList.Select(n => ((ProjectGraphNodeAdapter) n).AdaptedNode).ToImmutableArray());
        }
    }
}
