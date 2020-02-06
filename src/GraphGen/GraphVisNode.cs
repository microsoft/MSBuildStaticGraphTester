using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Graph;

namespace GraphGen
{
    public class GraphVisNode
    {
        // Ensure the same number is returned for the same ProjectGraphNode object
        private static readonly Dictionary<ProjectGraphNode, string> Nodes = new Dictionary<ProjectGraphNode, string>();
        public static int Count = 1;
        private readonly IEnumerable<string> _entryTargets;
        private readonly string _label;
        private readonly ProjectGraphNode _node;

        public string Name { get; }

        public GraphVisNode(ProjectGraphNode node, IEnumerable<string> entryTargets)
        {
            _node = node;
            _entryTargets = entryTargets;
            var (name, label) = GetNodeInfo(node);
            Name = name;
            _label = label;
        }

        internal string Create()
        {
            var globalPropertiesString = string.Join(
                "\n",
                _node.ProjectInstance.GlobalProperties.OrderBy(kvp => kvp.Key)
                    .Where(kvp => kvp.Key != "IsGraphBuild")
                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            if (globalPropertiesString.StartsWith("TargetFramework="))
            {
                globalPropertiesString = globalPropertiesString.Substring("TargetFramework=".Length);
            }

            var entryTargetsString = string.Join(";", _entryTargets);

            var label = new StringBuilder();

            label.Append("\"");

            label.Append(_label);

            if (!string.IsNullOrEmpty(globalPropertiesString))
            {
                label.Append($"\n{globalPropertiesString}");
            }

            if (!string.IsNullOrEmpty(entryTargetsString))
            {
                label.Append($"\n/t:{entryTargetsString}");
            }

            label.Append("\"");

            return $"  {Name} [label={label}, shape=box];"; //, color=\"0.650 0.200 1.000\"];";
        }

        private static (string, string) GetNodeInfo(ProjectGraphNode node)
        {
            // labels with '-' in them screw up graphvis so replace them with '_'
            var label = Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath).Replace("-", "_");

            if (!Nodes.ContainsKey(node))
            {
                Nodes.Add(node, label.Replace(".", string.Empty) + Count);
                Count++;
            }
            var name = Nodes[node];
            //var name = _current;//label + Program.HashGlobalProps(node.ProjectInstance.GlobalProperties);

            return (name, label);
        }
    }
}
