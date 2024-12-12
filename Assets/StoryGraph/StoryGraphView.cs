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
    /// GraphWindow�̒���Viewer
    /// </summary>
    public class StoryGraphView : GraphView
    {
        public RootNode RootNode;

        public Editor.StoryGraphDataContainer dataContainer;

        internal StoryGraphWindow graphWindow;

        internal StoryGraphSearchWindowProvider searchWindowProvider;

        public Vector2 mousePosition;
        /// <summary>
        /// StoryGraph�ɑ��݂��邷�ׂĂ�EventNode
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

            // �Y�[���@�\
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // View���̈ړ�
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            var dragger = new ContentDragger();
            
            // �w�i�F
            Insert(0, new GridBackground());
            // �����m�[�h�ǉ���
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());

            // Node�����@�\ 
            searchWindowProvider = ScriptableObject.CreateInstance<StoryGraphSearchWindowProvider>();
            searchWindowProvider.Initialize(this);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
            };
        }

       

        /// <summary>
        /// �m�[�h�Ԃ̐ڑ����[��
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            // �m�[�h�Ԃ̐ڑ����[��
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
        /// RootNode���쐬����
        /// </summary>
        public void CreateRootNodeIfNeeded()
        {
            if (nodes.ToList().Find(n => n is RootNode) != null)
                return;
            // Root node�������쐬
            RootNode = new RootNode();
            AddElement(RootNode);

            var rect = RootNode.GetPosition();
            rect.position = Vector3.zero;
            RootNode.SetPosition(rect);

            RootNode.Focus();
        }

        /// <summary>
        /// SafeDataInfo(�Z�[�u���ꂽ�f�[�^)��Event�̐i�s�x�������eEventNode�ɐݒu���� (Execute)����ۂɕK�v
        /// </summary>
        /// <param name="data">Event�̃Z�[�u�f�[�^</param>
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
        /// <c>StorySaveData</c>�̌`�Ō��݂�Story�̐i�s�x�������擾
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
        /// ���s�\��Event���擾
        /// </summary>
        /// <param name="nodeID">�r������J�n����ꍇnodeID����J�n�����</param>
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
        /// ID����Event���������Ă����Node��Ԃ�
        /// </summary>
        /// <param name="id">EventID</param>
        /// <returns>�������ꂽEventNode</returns>
        public EventNode GetEventNode(string id)
        {
            return EventNodes.Find(n => n.DataContainer.EventID == id);
        }

        /// <summary>
        /// lastOutput���玟�̃C�x���gNodes���擾����
        /// </summary>
        /// <param name="lastOutput">EventGraph�̍Ō�̏o�͒l�A��������null�̏ꍇRootNode������s</param>
        public List<EventNode> GetNextEventNodes(EventGraph.InOut.EventOutput lastOutput)
        {
            Debug.Log("GetNextEventNode");
            var output = new List<EventNode>();
            if (lastOutput == null)
            {
                // RootNode������s�\��EventNode���擾
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