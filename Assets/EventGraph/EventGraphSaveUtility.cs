using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using EventGraph.Nodes;
using UnityEditor;
using EventGraph.Nodes.Parts;
using System.IO;

namespace EventGraph.Editor
{
    public class EventGraphSaveUtility
    {
        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<EventGraphDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///　渡したVIewのContentをセーブする
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphView"></param>
        public static bool SaveGraph(EventGraphView graphView)
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
                if (node is RootNode r)
                    graphView.dataContainer.EventID = r.EventID;
                var data = NodeFactory.MakeDataFromNode(node);
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
                    TargetPortName = edge.input.portName
                });
            }

            SaveWindowInfo(graphView);
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Windowの表示状態などのgraphに関係ないところのセーブを行う
        /// </summary>
        public static void SaveWindowInfo(EventGraphView graphView)
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
        public static EventGraphView LoadGraph(string path)
        {
            if (!File.Exists(path)) return null;
            var graphView = new EventGraphView();
            LoadGraph(path, graphView);
            return graphView;
        }

        /// <summary>
        /// PathからGraphDataContainerのassetファイルを読み込み
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphView"></param>
        public static void LoadGraph(string path, EventGraphView graphView)
        {
            var dataContainer = AssetDatabase.LoadAssetAtPath<EventGraphDataContainer>(path);
            LoadGraph(dataContainer, graphView);
        }

        /// <summary>
        /// graphViewにDataContainerの内容を展開する
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="graphView"></param>
        public static EventGraphView LoadGraph(EventGraphDataContainer dataContainer, EventGraphView graphView = null)
        {
            if (graphView == null)
                graphView = new EventGraphView();

            ClearGraph(graphView);
            CreateNodes(graphView, dataContainer);
            CreateEdges(graphView, dataContainer);

            if (graphView.nodes.ToList().Find(n => n is RootNode) == null)
            {
                // RootNodeが存在しない場合は作成する
                graphView.CreateRootNode();
            }

            graphView.dataContainer = dataContainer;
            graphView.UpdateViewTransform(dataContainer.viewPosition, dataContainer.viewScale);
            // ApplyExpandedState(graphView, graphData); // NodeがExpanded状態でないとPortを発見できないのでEdge生成後に折りたたむ

            EventGraphView output = graphView;
            return output;
        }

        private static void ClearGraph(EventGraphView graphView)
        {
            graphView.nodes.ToList().ForEach(graphView.RemoveElement);
            graphView.edges.ToList().ForEach(graphView.RemoveElement);
        }

        private static void CreateNodes(EventGraphView graphView, EventGraphDataContainer graphData)
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

        private static void CreateEdges(EventGraphView graphView, EventGraphDataContainer graphData)
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