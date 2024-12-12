using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using EventGraph.Nodes;
using UnityEngine;
using UnityEditor;
using static Utility;

namespace EventGraph
{
    /// <summary>
    /// GraphWindowの中のViewer
    /// </summary>
    public class EventGraphView : GraphView
    {
        public RootNode RootNode;

        public Editor.EventGraphDataContainer DataContainer;

        internal EventGraphWindow graphWindow;

        internal EventGraphSearchWindowProvider searchWindowProvider;

        internal Vector2 mousePosition;
        // 説明ポップアップが表示されるまでの時間
        private const float SHOW_POPUP_TIME = 1.5f;
        // カーソルがNodeの上に乗ったときの時間
        private float mouseOverTime;
        // カーソルが乗っているNode
        private Node mouseOverNode;

        public EventGraphView() : base()
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
            searchWindowProvider = ScriptableObject.CreateInstance<EventGraphSearchWindowProvider>();
            searchWindowProvider.Initialize(this);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
            };

            //graphWindow = (EventGraphWindow)EditorWindow.GetWindow(typeof(EventGraphWindow));
        }

        [Obsolete]
        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);
            // マウスの位置がNodeの上にあるかどうか
            if (evt is MouseMoveEvent mouseMoveEvent)
            {
                mousePosition = mouseMoveEvent.mousePosition;
                var sampleNodes = nodes.ToList().Where(n => n is SampleNode).Select(n => (SampleNode)n);
                foreach (var node in sampleNodes)
                {
                    // マウスがNodeの上にあり、かつNodeが選択されている
                    if (node.ContainsPoint(node.WorldToLocal(mouseMoveEvent.mousePosition)) && selection.Contains(node))
                    {
                        // カーソルが同じNodeの上に乗っている時間を計測
                        if (mouseOverNode == node)
                        {
                            mouseOverTime += Time.deltaTime;
                            // 一定時間以上カーソルが同じNodeの上に乗っていたら説明ポップアップを表示
                            if (mouseOverTime > SHOW_POPUP_TIME && 
                                !graphWindow.IsDescriptionPopupOpen && 
                                node.Description.Length > 0)
                            {
                                // 説明ポップアップを表示
                                graphWindow.ShowNodeDescription(node);
                            }
                        }
                        else
                        {
                            // カーソルがNodeの上に乗っている時間をリセット
                            mouseOverTime = 0;
                            mouseOverNode = node;
                        }
                    }
                    
                }
            }
            
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
        public List<InOut.EventOutput> Execute(InOut.EventInput input)
        {
            if (RootNode == null)
            {
                Debug.Log("RootNode is null");
                return new List<InOut.EventOutput>();
            }

            var output = new List<InOut.EventOutput>();

            Edge firstEdge = null;
            if (input.StartAtID != null && input.StartAtID.Length != 0)
            {
                var startNode = (ProcessNode)nodes.ToList().Find(n => ((SampleNode)n).Guid == input.StartAtID);
                if (startNode == null)
                {
                    // StartAtIDが見つからない場合は異なるEventViewを参照しているということ
                    return output;
                }
                var _output = startNode.ExecuteFromMiddle(input);
                if (_output != null)
                {
                    _output.EventID = RootNode.EventID;
                    if (_output.NextPort != null)
                        firstEdge = _output.NextPort.connections.FirstOrDefault();
                    //output.Add(_output);
                }
            }
            else
            {
                firstEdge = RootNode.OutputPort.connections.FirstOrDefault();
                var evt = RootNode.Execute(input);
                evt.EventID = RootNode.EventID;
                output.Add(evt);
            }
            

            if (firstEdge == null) return output;

            var currentNode = firstEdge.input.node as ProcessNode;

            while (true)
            {
                var evt = currentNode.Execute(input);
                evt.EventID = RootNode.EventID;
                output.Add(evt);

                if (evt == null || evt.NextPort == null)
                    break;
                var edge = evt.NextPort.connections.FirstOrDefault();
                if (edge == null) break;

                currentNode = edge.input.node as ProcessNode;
            }

            return output;
        }

    }
}