using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using StoryGraph.Nodes;
using UnityEngine;
using static Utility;

namespace StoryGraph
{
    /// <summary>
    /// GraphWindowの中のViewer
    /// </summary>
    public class StoryGraphView : GraphView
    {
        public RootNode RootNode;

        public Editor.StoryGraphDataContainer dataContainer;

        internal StoryGraphWindow graphWindow;

        internal StoryGraphSearchWindowProvider searchWindowProvider;

        public Vector2 mousePosition;
        /// <summary>
        /// StoryGraphに存在するすべてのEventNode
        /// </summary>
        public List<EventNode> EventNodes
        {
            get
            {
                return nodes.ToList().FindAll(n => n is EventNode).ConvertAll(n => (EventNode)n);
            }
        }

        public StoryGraphView() : base()
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
            searchWindowProvider = ScriptableObject.CreateInstance<StoryGraphSearchWindowProvider>();
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
        public void CreateRootNodeIfNeeded()
        {
            if (nodes.ToList().Find(n => n is RootNode) != null)
                return;
            // Root nodeを自動作成
            RootNode = new RootNode();
            AddElement(RootNode);

            var rect = RootNode.GetPosition();
            rect.position = Vector3.zero;
            RootNode.SetPosition(rect);

            RootNode.Focus();
        }

        /// <summary>
        /// SafeDataInfo(セーブされたデータ)のEventの進行度合いを各EventNodeに設置する (Execute)する際に必要
        /// </summary>
        /// <param name="data">Eventのセーブデータ</param>
        public void SetSaveData(List<SaveDataInfo.StorySaveData.EventSaveData> data)
        {
            EventNodes.ForEach(n =>
            {
                var saveData = data.Find(d => d.EventID == n.DataContainer.EventID);
                if (saveData != null)
                    n.SaveData = saveData.Clone();
                else
                    n.SaveData = new SaveDataInfo.StorySaveData.EventSaveData(n.DataContainer.EventID);
            });
        }

        /// <summary>
        /// <c>StorySaveData</c>の形で現在のStoryの進行度合いを取得
        /// </summary>
        public SaveDataInfo.StorySaveData GetSaveData()
        {
            var data = new SaveDataInfo.StorySaveData(dataContainer.StoryID);
            EventNodes.ForEach(e =>
            {
                data.EventsData.Add(e.SaveData);
            });
            return data;
        }

        /// <summary>
        /// 実行可能なEventを取得
        /// </summary>
        /// <param name="nodeID">途中から開始する場合nodeIDから開始される</param>
        public List<EventNode> GetRunableNodes()
        {
            var runningNodes = EventNodes.FindAll(n => n.SaveData.state == SaveDataInfo.StorySaveData.EventSaveData.State.Running);
            if (runningNodes.Count == 0)
            {
                RootNode.OutputPort.connections.ToList().ForEach(e =>
                {
                    if (e.input.node is EventNode eventNode && !eventNode.IsCompleted)
                    {
                        eventNode.SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Running;
                        runningNodes.Add(eventNode);
                    }
                });
            }
            return runningNodes;
        }

        /// <summary>
        /// IDからEventを検索してこれのNodeを返す
        /// </summary>
        /// <param name="id">EventID</param>
        /// <returns>検索されたEventNode</returns>
        public EventNode GetEventNode(string id)
        {
            return EventNodes.Find(n => n.DataContainer.EventID == id);
        }

        /// <summary>
        /// lastOutputから次のイベントNodesを取得する
        /// </summary>
        /// <param name="lastOutput">EventGraphの最後の出力値、もしくはnullの場合RootNodeから実行</param>
        public List<EventNode> GetNextEventNodes(EventGraph.InOut.EventOutput lastOutput)
        {
            Debug.Log("GetNextEventNode");
            var output = new List<EventNode>();
            if (lastOutput == null)
            {
                // RootNodeから実行可能なEventNodeを取得
                RootNode.OutputPort.connections.ToList().ForEach(e =>
                {
                    if (e.input.node is EventNode eventNode)
                    {
                        Debug.Log("SetRunning");
                        eventNode.SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Running;
                        output.Add(eventNode);
                    }
                });
            }
            else if (lastOutput.IsEventCompleted)
            {
                var lastEventNode = EventNodes.Find(e => e.DataContainer.EventID == lastOutput.EventID);
                if (lastEventNode == null)
                {
                    return output;
                }
                lastEventNode.SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Completed;

                var port = lastEventNode.FindOutputPortFromEndEventName(lastOutput.EndEventName);
                if (!port.connected)
                    return output;
                port.connections.ToList().ForEach(e =>
                {
                    if (e.input.node is EventNode eventNode)
                    {
                        eventNode.SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Running;
                        Debug.Log("SetRunning");
                        output.Add(eventNode);
                    }
                });
            }

            return output;
        }

    }
}