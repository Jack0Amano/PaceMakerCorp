using System;
using System.Linq;
using AIGraph.Nodes;
using AIGraph.Editor;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using AIGraph.InOut;
using Unity.Logging;
using static Utility;
using Tactics.Map;
using UnityEngine.Rendering.VirtualTexturing;
using Unity.Logging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager.UI;

namespace AIGraph
{
    /// <summary>
    /// GraphWindowの中のViewer
    /// </summary>
    public class AIGraphView : GraphView
    {
        public RootNode RootNode;

        public Editor.AIGraphDataContainer dataContainer;

        internal AIGraphSearchWindowProvider searchWindowProvider;
        /// <summary>
        /// Executeした際の色変化させる色
        /// </summary>
        public static Color RunGraphColor = ColorExtensions.Rgb256(55, 235, 52);

        public Vector2 mousePosition;
        /// <summary>
        /// Situationのlistの計算した残り
        /// </summary>
        internal Dictionary<PointInTile, Situation> SituationsDictionary;
        /// <summary>
        /// 実行時に開かれるデバッグ用のGraphWindow
        /// </summary>
        private AIGraphWindow DebugWindow;

        public AIGraphView() : base()
        {
            GridBackground gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            // ズーム機能
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // View内の移動
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            var dragger = new ContentDragger();

            // 背景色
            Insert(0, new GridBackground());
            // 複数ノード追加可
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());

            // Node検索機能 
            searchWindowProvider = ScriptableObject.CreateInstance<AIGraphSearchWindowProvider>();
            searchWindowProvider.Initialize(this);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
            };
        }



        /// <summary>
        /// ノード間の接続ルール
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            // ノード間の接続ルール
            var compatiblePorts = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (!port.enabledSelf)
                    continue;

                // List<object>を受け取りportとして設定した場合List<>であればすべて受け取る
                if (startAnchor.node != port.node &&
                    startAnchor.direction != port.direction &&
                    IsList(startAnchor.portType) && port.portType == typeof(List<object>))
                {
                    compatiblePorts.Add(port);
                    continue;
                }

                // portType == object の場合はどのPortでも接続できる
                if (startAnchor.node != port.node &&
                    startAnchor.direction != port.direction &&
                    (port.portType == typeof(object) || startAnchor.portType == typeof(object)))
                {
                    compatiblePorts.Add(port);
                    continue;
                }

                if (startAnchor.node == port.node ||
                    startAnchor.direction == port.direction ||
                    startAnchor.portType != port.portType)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }
            return compatiblePorts;
        }

        /// <summary>
        /// 与えられたtがlistであるか
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool IsList(Type t)
        {
            if (!t.IsGenericType)
                return false;
            return typeof(List<>).IsAssignableFrom(t.GetGenericTypeDefinition());
        }

        /// <summary>
        /// RootNodeを作成する
        /// </summary>
        public void CreateRootNode()
        {
            // Root nodeを自動作成
            RootNode = new RootNode();
            AddElement(RootNode);
        }

        /// <summary>
        /// Nodeを実行
        /// </summary>
        /// <param name="nodeID">途中から開始する場合nodeIDから開始される</param>
        /// <returns>Nodeを実行した際の結果 (Nodeが進まなかった場合Empty)</returns>
        internal AIAction Execute(EnvironmentData data, bool openDebugWindow = false)
        {
            if (openDebugWindow)
            {
                if (DebugWindow == null)
                {
                    DebugWindow = EditorWindow.CreateWindow<AIGraphWindow>();
                    DebugWindow.IsDebugMode = true;
                    DebugWindow.titleContent.text = "Runtime graph";
                }
                DebugWindow.GraphView = this;
                DebugWindow.DrawGraph();
                DebugWindow.Show();
            }

            SituationsDictionary = new Dictionary<PointInTile, Situation>();

            var output = new AIAction();

            RootNode = (RootNode)nodes.ToList().Find(n => n is RootNode);
            ProcessNode currentNode = RootNode;
            var outputPort = RootNode.OutputPort;
            while (true)
            {
                currentNode.AIGraphView = this;

                Log.Info($"Start to run node of ${currentNode}");
                var outData = currentNode.Execute(data);
                currentNode.DebugDraw(outData);
                outputPort = outData.OutPort;
                if (outputPort == null)
                {
                    if (currentNode is EndAINode)
                    {
                        output = currentNode.AIAction;
                        break;
                    }
                    else
                        Log.Error($"End of edge must be an EndAINode\nEnd at {currentNode}");
                }
                else if (outputPort.ConnectedNodes.Count == 1)
                {
                    if ((ProcessNode)outputPort.ConnectedNodes.FirstOrDefault() != currentNode)
                        currentNode = (ProcessNode)outputPort.ConnectedNodes[0];
                    else
                    {
                        Log.Error($"{currentNode} sets same node in Execute().OutPort. It's must be roop");
                        break;
                    }
                }
                else
                    Log.Error($"The port of the {currentNode} are connected to two or more nodes. \nOnly one connection to the Node of Port of ProcessNode is allowed.");
            }

            Log.Info($"AIGraphView output: {output}");
            return output;
        }

        /// <summary>
        /// デバッグ用のWindowがある場合これを閉じる
        /// </summary>
        public void DestroyDebugGraphWindow()
        {
            if (DebugWindow != null)
                DebugWindow.Close();
        }
    }

}