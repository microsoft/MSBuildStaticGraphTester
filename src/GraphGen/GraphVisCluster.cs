using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GraphGen
{
    public class GraphVisCluster
    {
        private static int _clusterIdGlobal;
        private static int _clusterId;

        private readonly List<GraphVisNode> _nodes = new List<GraphVisNode>();
        private readonly string _clusterLabel;

        public GraphVisCluster(string projectPath)
        {
            _clusterLabel = Path.GetFileName(projectPath);
            _clusterId = _clusterIdGlobal;
            _clusterIdGlobal++;
        }

        public void AddNode(GraphVisNode node)
        {
            _nodes.Add(node);
        }

        public string Create()
        {
            if (_nodes.Count == 1)
            {
                return _nodes[0].Create();
            }

            var result = $@"
        subgraph cluster_{_clusterId} {{
		style=filled;
		color=lightgrey;
		node [style=filled,color=white];
		label = ""{_clusterLabel}"";";

            foreach (var p in _nodes)
            {
                result += p.Create();
            }

            result += "}";
            return result;

        }

    }
}
