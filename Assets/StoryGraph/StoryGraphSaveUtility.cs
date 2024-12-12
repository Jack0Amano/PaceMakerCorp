using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using StoryGraph.Nodes;
using UnityEditor;
using StoryGraph.Nodes.Parts;
using System.IO;

namespace StoryGraph.Editor
{
    public class StoryGraphSaveUtility
    {
        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<StoryGraphDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///�@�n����VIew��Content���Z�[�u����
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphView"></param>
        public static bool SaveGraph(StoryGraphView graphView)
        {
            if (graphView.dataContainer == null)
            {
                Debug.LogWarning($"DataContainer is null");
                return false;
            }

            var nodes = GetNodes(graphView);
            graphView.dataContainer.Nodes.Clear();

            foreach(var node in nodes)
            {
                if (node.Guid == null || node.Guid.Length == 0)
                    node.Guid = System.Guid.NewGuid().ToString();
                var data = NodeFactory.MakeDataFromNode(node);
                if (node is RootNode r)
                    graphView.dataContainer.StoryID = r.StoryID;
                if (data == null)
                {
                    Debug.LogWarning($"{data} is missing. The node isn't saved.");
                    continue;
                }
                graphView.dataContainer.Nodes.Add(data);
            }

            var edges = GetEdges(graphView);
            graphView.dataContainer.Edges.Clear();
            foreach(var edge in edges)
            {
                if (edge.output.node == null || edge.input.node == null)
                {
                    Debug.LogWarning($"Edge from {edge.output.node} to {edge.input.node} is not connected. The edge isn't saved.");
                    continue;
                }
                var outputNode = edge.output.node as SampleNode;
                var inputNode = edge.input.node as SampleNode;

                graphView.dataContainer.Edges.Add(new EdgeData()
                {
                    BaseNodeGuid = outputNode.Guid,
                    BasePortName = edge.output.portName,
                    TargetNodeGuid = inputNode.Guid,
                    TargetPortName = edge.input.portName,
                });
            }

            SaveWindowInfo(graphView);
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Window�̕\����ԂȂǂ�graph�Ɋ֌W�Ȃ��Ƃ���̃Z�[�u���s��
        /// </summary>
        public static void SaveWindowInfo(StoryGraphView graphView)
        {
            // View�̃|�W�V�����ƃY�[����ۑ�
            graphView.dataContainer.viewPosition = graphView.viewTransform.position;
            graphView.dataContainer.viewScale = graphView.viewTransform.scale;
            EditorUtility.SetDirty(graphView.dataContainer);
           
        }

        /// <summary>
        /// Path����GraphDataContainer��asset�t�@�C����ǂݍ���
        /// </summary>
        /// <param name="path"></param>
        public static StoryGraphView LoadGraph(string path)
        {
            if (!File.Exists(path)) return null;
            var graphView = new StoryGraphView();
            LoadGraph(path, graphView);
            return graphView;
        }

        /// <summary>
        /// Path����GraphDataContainer��asset�t�@�C����ǂݍ���
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphView"></param>
        public static void LoadGraph(string path, StoryGraphView graphView)
        {
            var dataContainer = AssetDatabase.LoadAssetAtPath<StoryGraphDataContainer>(path);
            if (graphView == null)
            {
                EditorUtility.DisplayDialog("FIle Not Found", "Target graph file does not exists!", "OK");
                return;
            }

            LoadGraph(dataContainer, graphView);
        }

        /// <summary>
        /// DataContainer�̓��e����graphView���쐬����
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="graphView"></param>
        public static StoryGraphView LoadGraph(StoryGraphDataContainer dataContainer, StoryGraphView graphView = null)
        {
            if (graphView == null)
                graphView = new StoryGraphView();

            ClearGraph(graphView);
            CreateNodes(graphView, dataContainer);
            CreateEdges(graphView, dataContainer);

            // RootNode�����݂��Ȃ��ꍇ�͍쐬����
            graphView.CreateRootNodeIfNeeded();

            graphView.dataContainer = dataContainer;
            graphView.UpdateViewTransform(dataContainer.viewPosition, dataContainer.viewScale);
            // ApplyExpandedState(graphView, graphData); // Node��Expanded��ԂłȂ���Port�𔭌��ł��Ȃ��̂�Edge������ɐ܂肽����

            return graphView;
        }

        private static void ClearGraph(StoryGraphView graphView)
        {
            graphView.nodes.ToList().ForEach(graphView.RemoveElement);
            graphView.edges.ToList().ForEach(graphView.RemoveElement);
        }

        private static void CreateNodes(StoryGraphView graphView, StoryGraphDataContainer graphData)
        {
            foreach (var nodeData in graphData.Nodes)
            {
                var tempNode = NodeFactory.MakeNodeFromData(nodeData);
                if (tempNode == null)
                {
                    Debug.LogWarning($"{nodeData} is missing");
                    continue;
                }
                graphView.AddElement(tempNode);

                if (tempNode is RootNode rootNode)
                    graphView.RootNode = rootNode;
            }
        }

        private static void CreateEdges(StoryGraphView graphView, StoryGraphDataContainer graphData)
        {
            var nodes = GetNodes(graphView);
            foreach (var baseNode in nodes)
            {
                var edges = graphData.Edges.Where(x => x.BaseNodeGuid == baseNode.Guid).ToList();
                foreach (var edgeData in edges)
                {

                    var targetNode = nodes.FirstOrDefault(x => x.Guid == edgeData.TargetNodeGuid);
                    if (targetNode == default)
                    {
                        Debug.LogWarning($"TargetNode is missin in {edgeData}");
                        continue;
                    }
                    var inputPort = targetNode.inputContainer.Query<CustomPort>().ToList().Find(p => p.portName == edgeData.TargetPortName);
                    var outputPort = baseNode.outputContainer.Query<CustomPort>().ToList().Find(p => p.portName == edgeData.BasePortName); 

                    if (inputPort == null || outputPort == null)
                    {
                        Debug.LogWarning($"{edgeData} is not connected");
                        continue;
                    }

                    var edge = ConnectPorts(outputPort, inputPort);
                    graphView.Add(edge);
                }
            }
        }

        private static Edge ConnectPorts(CustomPort output, CustomPort input)
        {
            var tempEdge = new Edge
            {
                output = output,
                input = input
            };
            output.Connect(tempEdge);
            input.Connect(tempEdge);
            return tempEdge;
        }

        //private static void ApplyExpandedState(graphView, graphData)
        //{
        //    // ��
        //}

        private static List<Edge> GetEdges(GraphView graphView) => graphView.edges.ToList();
        private static List<SampleNode> GetNodes(GraphView graphView) => graphView.nodes.ToList().Cast<SampleNode>().ToList();
    }


    /// <summary>
    /// NodeData���Node���쐬����
    /// </summary>
    public class NodeFactory
    {

        /// <summary>
        /// NodeData���Node���쐬���� 
        /// </summary>
        /// <param name="data">Serialize����ĕۑ�����Ă���Nod�̃f�[�^</param>
        /// <returns>��������Node</returns>
        /// Keyward����Node���������� �ʒu���̏�����͂��ĕԂ�
        public static SampleNode MakeNodeFromData(NodeData data)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = a.GetTypes()
                        .ToList()
                        .Find(t => t.IsClass
                        && !t.IsAbstract
                        && (t.IsSubclassOf(typeof(SampleNode)))
                        && t.Name == data.Keyword);
                if (type != null)
                {
                    var node = (SampleNode)Activator.CreateInstance(type);
                    node.Load(data);
                    return node;
                }
            }

            return null;
        }



        /// <summary>
        /// Node����NodeData���쐬���ĕԂ�
        /// </summary>
        /// <param name="node"><�f�[�^����Node/param>
        /// <returns>Serialize���ꂽ�ۑ��p�f�[�^</returns>
        public static NodeData MakeDataFromNode(SampleNode node)
        {
                return node.Save() ;
        }
    }
}