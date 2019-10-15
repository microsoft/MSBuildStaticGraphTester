using GraphGen;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MSBuildGraphUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            tbSaveFile.Text = Path.GetTempPath() + "MSB_GRAPH.png";
            tabControl1.SelectTab("tabPage2");
        }
        private void button1_Click(object sender, EventArgs e)
        {
            LoadGraph(new FileInfo(@"D:\src\msbuild.fork4\src\MSBuild\MSBuild.csproj"), tbSaveFile.Text);
        }

        private void _loadButton_Click(object sender, EventArgs e)
        {
            if (_openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadGraph(new FileInfo(_openFileDialog.FileName), tbSaveFile.Text);
            }

        }

        private async void LoadGraph(FileInfo project, string graphOutputFile)
        {
            _statusBarLabel.Text = $@"Loading {project.FullName}...";

            // Load the graph from MSBuild
            var stopwatch = Stopwatch.StartNew();
            var graph = await Task.Factory.StartNew(()=> new ProjectGraph(new []{new ProjectGraphEntryPoint(project.FullName)}, ProjectCollection.GlobalProjectCollection, ProjectInstanceFactory));
            stopwatch.Stop();
            _statusBarLabel.Text = $@"{project.Name} loaded {graph.ProjectNodes.Count} node(s) in {stopwatch.ElapsedMilliseconds}ms.";

            // Create the graph png file
            var options = new GraphVisOptions {NodeSep = (double)numNodeSep.Value, RankSep = (double)numRankSep.Value};
            var graphText = await Task.Factory.StartNew(() => GraphVis.Create(graph, options));
            await Task.Factory.StartNew(() => GraphVis.Save(graphText, graphOutputFile));
            webBrowser1.Url = new Uri(graphOutputFile);


            // Populate the tree view
            _statusBarLabel.Text = $@"{project.Name} loaded {graph.ProjectNodes.Count} node(s) in {stopwatch.ElapsedMilliseconds}ms. Populating the TreeView...";
            var stopwatch2 = Stopwatch.StartNew();
            await Task.Factory.StartNew(() => PopulateTree(graph));
            stopwatch2.Stop();
            _statusBarLabel.Text = $@"{project.Name} loaded {graph.ProjectNodes.Count} node(s) in {stopwatch.ElapsedMilliseconds}ms. {stopwatch2.ElapsedMilliseconds}ms to draw {_counts} nodes in the TreeView.";
        }

        private ProjectInstance ProjectInstanceFactory(string projectFile, Dictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            Invoke(new Action(() => _statusBarLabel.Text = $@"Loading {projectFile}..."));

            var sw = Stopwatch.StartNew();
            var pi = new ProjectInstance(
                projectFile,
                globalProperties,
                "Current",
                projectCollection);
            sw.Stop();

            Invoke(new Action(() => _statusBarLabel.Text = $@"Loading {projectFile}. Done in {sw.ElapsedMilliseconds}ms"));

            return pi;
        }

        private void PopulateTree(ProjectGraph graph)
        {
            Invoke(new Action(() => _treeVew.Nodes.Clear()));

            foreach (var root in graph.GraphRoots)
            {
                Invoke(new Action(() => _treeVew.Nodes.Add(AdaptGraphNode(root))));
                
            }
        }

        private int _counts = 0;

        private TreeNode AdaptGraphNode(ProjectGraphNode node)
        {
            Interlocked.Increment(ref _counts);
            TreeNode treeNode = new TreeNode(node.ProjectInstance.FullPath) {Tag = node};

            foreach (var child in node.ProjectReferences)
            {
                treeNode.Nodes.Add(AdaptGraphNode(child));
            }

            return treeNode;
        }

        private void _treeVew_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var graphNode = (ProjectGraphNode) e.Node.Tag;
            //_propertyGrid.SelectedObject = new { Name = graphNode.ProjectInstance.FullPath, Props = graphNode.ProjectInstance.GlobalProperties};
            _propertyGrid.SelectedObject = graphNode.ProjectInstance;
        }
    }
}
