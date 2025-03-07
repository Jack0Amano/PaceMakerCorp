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
        ///　渡したVIewのContentをセーブする
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
        /// Windowの表示状態などのgraphに関係ないところのセーブを行う
        /// </summary>
        public static void SaveWindowInfo(StoryGraphView graphView)
        {
            // Viewのポジションとズームを保存
            graphView.dataContainer.viewPosition = graphView.viewTransform.position;
            graphView.dataContainer.viewScale = graphView.viewTransform.scale;
            EditorUtility.SetDirty(graphView.dataContainer);
           
        }

        /// <summary>
        /// PathからGraphDataContainerのassetファイルを読み込み
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
        /// PathからGraphDataContainerのassetファイルを読み込み
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
        /// DataContainerの内容からgraphViewを作成する
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

            // RootNodeが存在しない場合は作成する
            graphView.CreateRootNodeIfNeeded();

            graphView.dataContainer = dataContainer;
            graphView.UpdateViewTransform(dataContainer.viewPosition, dataContainer.viewScale);
            // ApplyExpandedState(graphView, graphData); // NodeがExpanded状態でないとPortを発見できないのでEdge生成後に折りたたむ

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
        //    // 略
        //}

        private static List<Edge> GetEdges(GraphView graphView) => graphView.edges.ToList();
        private static List<SampleNode> GetNodes(GraphView graphView) => graphView.nodes.ToList().Cast<SampleNode>().ToList();
    }


    /// <summary>
    /// NodeDataよりNodeを作成する
    /// </summary>
    public class NodeFactory
    {

        /// <summary>
        /// NodeDataよりNodeを作成する 
        /// </summary>
        /// <param name="data">Serializeされて保存されているNodのデータ</param>
        /// <returns>復元したNode</returns>
        /// KeywardからNodeを検索して 位置等の情報を入力して返す
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
        /// NodeからNodeDataを作成して返す
        /// </summary>
        /// <param name="node"><データ元のNode/param>
        /// <returns>Serializeされた保存用データ</returns>
        public static NodeData MakeDataFromNode(SampleNode node)
        {
                return node.Save() ;
        }
    }
}